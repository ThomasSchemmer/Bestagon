using System.Collections;
using System.Collections.Generic;
using System.Runtime.Remoting.Lifetime;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Windows;
using static HexagonConfig;

public class WorldGenerator : GameService
{

    struct BiomeStruct
    {
        public Vector2 Position;
        public Vector2 Size;
        public uint BiomeIndex;
    }

    struct RangeStruct
    {
        public Vector2 Range;
        public uint BiomeIndex;
    }
    
    public struct HexagonInfo
    {
        public float Height;
        public float Temperature;
        public float Humidity;

        public HexagonInfo(float a, float b, float c)
        {
            Height = a;
            Temperature = b;
            Humidity = c;
        }
    }

    protected override void StartServiceInternal() {
        Init();
        IsInit = true;
        _OnInit?.Invoke();
    }

    protected override void StopServiceInternal() { }

    public Tile[] EmptyLand()
    {
        if (!IsInit)
            return new Tile[0];

        Tile[] LandData = new Tile[MapWidth * MapWidth];
        // set to water at meadow temperature
        Tile EmptyTile = GetTileFromMapValue(new HexagonInfo(0.1f, 0.6f, 0f));
        System.Array.Fill(LandData, EmptyTile);
        return LandData;
    }

    public Tile[] NoiseLand(bool bIncludeHumidity) {
        Init();

        MapShader.Dispatch(HeightTemperatureKernel, GroupCount, GroupCount, 1);
        int Count = bIncludeHumidity ? (int)Mathf.Log(ImageWidth, 2) : 0;
        int StepSize = ImageWidth / 2;
        for (int i = 0; i < Count; i++) 
        {
            SetDataJumpFlood(StepSize, i);
            MapShader.Dispatch(JumpFloodKernel, GroupCount, GroupCount, 1);
            StepSize /= 2;
        }

        return GetMapData();
    }

    public Tile GetTileFromMapValue(HexagonInfo HexagonInfo)
    {
        return new Tile(
            GetHeightFromMapValue(HexagonInfo),
            GetTypeFromMapValue(HexagonInfo)
        );
    }

    public HexagonHeight GetHeightFromMapValue(HexagonInfo HexagonInfo)
    {
        BiomeMap.TryGetHexagonHeightForHeight(HexagonInfo.Height, out HexagonHeight Height);
        return Height;
    }

    public HexagonType GetTypeFromMapValue(HexagonInfo HexagonInfo)
    {
        BiomeMap.TryGetHexagonHeightForHeight(HexagonInfo.Height, out HexagonHeight HexHeight);
        if (BiomeMap.TryGetHexagonTypeForHeightOverride(HexHeight, out HexagonType Override))
            return Override;

        BiomeMap.TryGetHexagonTypeForClimate(new Climate(HexagonInfo.Temperature, HexagonInfo.Humidity), out HexagonType Land);
        return Land;
    }

    private void OnDestroy() {
        Release();
    }

    private void Release() {
        if (InputRT != null)
            InputRT.Release();
        if (OutputRT != null)
            OutputRT.Release();
        if (HexagonInfoBuffer != null)
            HexagonInfoBuffer.Release();
        if (ClimateBuffer != null) 
           ClimateBuffer.Release();
        if (HeightOverrideBuffer != null) 
            HeightOverrideBuffer.Release();
    }

    private void Init() {
        Release();

        CreateTempRT();
        InitBuffers();

        SetDataGlobal();
        SetDataHeightTemperature();
        // jump fill is set for each iteration
        SetDataType();

        FillBuffers();
    }

    private void InitBuffers() {
        HeightTemperatureKernel = MapShader.FindKernel("HeightTemperatureCalculation");
        JumpFloodKernel = MapShader.FindKernel("JumpFlood");
        TypeKernel = MapShader.FindKernel("TypeCalculation");
        HexagonInfoBuffer = new ComputeBuffer(ImageWidth * ImageWidth, sizeof(float) * 3);
        ClimateBuffer = new ComputeBuffer(BiomeMap.ClimateMap.Count, sizeof(float) * 4 + sizeof(uint));
        HeightOverrideBuffer = new ComputeBuffer(BiomeMap.HeightOverrideMap.Count, sizeof(float) * 2 + sizeof(uint));
    }

    private void SetDataHeightTemperature() {
        MapShader.SetFloat("Seed", Seed);
        MapShader.SetFloat("Scale", Scale);
        MapShader.SetFloat("Factor", Factor);
        MapShader.SetFloat("Offset", Offset);
        MapShader.SetFloat("Amount", Amount);

        MapShader.SetInt("TemperatureCount", BiomeMap.ClimateMap.Count);
        MapShader.SetInt("HeightCount", BiomeMap.HeightOverrideMap.Count);

        SetBuffersForKernel(HeightTemperatureKernel);
    }

    private void SetDataJumpFlood(int StepSize, int StepCount)
    {
        MapShader.SetInt("StepSize", StepSize);
        MapShader.SetFloat("Decay", HumidityDecay);

        bool IsEven = StepCount % 2 == 0;

        SetBuffersForKernel(JumpFloodKernel);
        MapShader.SetTexture(JumpFloodKernel, "InputImage", IsEven ? InputRT : OutputRT);
        MapShader.SetTexture(JumpFloodKernel, "OutputImage", IsEven ? OutputRT : InputRT);
    }

    private void SetDataType()
    {

        SetBuffersForKernel(TypeKernel);
    }

    private void SetDataGlobal()
    {
        MapShader.SetInt("GroupCount", GroupCount);
        MapShader.SetInt("Width", ImageWidth);
    }

    private void SetBuffersForKernel(int Kernel)
    {
        MapShader.SetBuffer(Kernel, "TemperatureMap", ClimateBuffer);
        MapShader.SetBuffer(Kernel, "HeightMap", HeightOverrideBuffer);
        MapShader.SetBuffer(Kernel, "HexagonInfos", HexagonInfoBuffer);
        MapShader.SetTexture(Kernel, "BiomeColors", BiomeColors);
        MapShader.SetTexture(Kernel, "InputImage", InputRT);
        MapShader.SetTexture(Kernel, "OutputImage", OutputRT);
    }

    private void CreateTempRT()
    {
        InputRT = new RenderTexture(ImageWidth, ImageWidth, 0, RenderTextureFormat.ARGB32);
        InputRT.enableRandomWrite = true;
        InputRT.Create();
        InputRT.filterMode = FilterMode.Point;
        InputRT.wrapMode = TextureWrapMode.Clamp;

       OutputRT = new RenderTexture(ImageWidth, ImageWidth, 0, RenderTextureFormat.ARGB32);
       OutputRT.enableRandomWrite = true;
       OutputRT.Create();
       OutputRT.filterMode = FilterMode.Point;
       OutputRT.wrapMode = TextureWrapMode.Clamp;
    }

    private void FillBuffers() {

        HexagonInfo[] Map = new HexagonInfo[ImageWidth * ImageWidth];
        List<BiomeStruct> Climates = new();
        List<RangeStruct> HeightOverrides = new();
        foreach (Biome Biome in BiomeMap.ClimateMap)
        {
            Climates.Add(new BiomeStruct()
            {
                Position = Biome.Rect.position,
                Size = Biome.Rect.size,
                BiomeIndex = (uint)MaskToInt((int)Biome.HexagonType, 16)
            });
        }
        foreach (var Tuple in BiomeMap.HeightMap)
        {
            if (!BiomeMap.HeightOverrideMap.ContainsKey(Tuple.Value))
                continue;

            HexagonType OverrideType = BiomeMap.HeightOverrideMap[Tuple.Value];

            HeightOverrides.Add(new RangeStruct()
            {
                Range = new Vector2(Tuple.Key.Min, Tuple.Key.Max),
                BiomeIndex = (uint)MaskToInt((int)OverrideType, 16)
            });
        }

        ClimateBuffer.SetData(Climates);
        HeightOverrideBuffer.SetData(HeightOverrides);
        HexagonInfoBuffer.SetData(Map);
    }

    private Tile[] GetMapData()
    {
        HexagonInfo[] ImageData = new HexagonInfo[ImageWidth * ImageWidth];
        HexagonInfoBuffer.GetData(ImageData);

        // convert image data (x: height, y: temp) to actual type
        // aspect ratio changes!
        float SizeMultiplier = (float)ImageWidth / MapWidth;
        Tile[] MapData = new Tile[MapWidth * MapWidth];
        for (int i = 0; i < MapData.Length; i++)
        {
            Vector2Int MapPos = new(i % MapWidth, i / MapWidth);
            Vector2Int ImagePos = new((int)(MapPos.x * SizeMultiplier), (int)(MapPos.y * SizeMultiplier));
            int ImageIndex = ImagePos.y * ImageWidth + ImagePos.x;
            HexagonInfo Value = ImageData[ImageIndex];
            MapData[i] = GetTileFromMapValue(Value);
        }

        return MapData;
    }


    public BiomeMap BiomeMap;

    public ComputeShader MapShader;
    public RenderTexture InputRT, OutputRT;
    public Texture2D BiomeColors;

    // how many iterations of noise
    public float Amount = 1;
    // how zoomed-in the noise should be
    public float Scale = 1;
    // how strong the noise should be
    public float Factor = 1;
    public float Offset = 0;
    public float Seed = 0;
    public float HumidityDecay = 5;

    private int HeightTemperatureKernel, JumpFloodKernel, TypeKernel;
    private ComputeBuffer HexagonInfoBuffer;
    private ComputeBuffer ClimateBuffer;
    private ComputeBuffer HeightOverrideBuffer;

    // to make compute calculations easier, make sure that GroupCount * NumThreads = RT.width!
    private static int ImageWidth = 256;
    private static int GroupCount = ImageWidth / 16;



    public void TestJumpFlood()
    {
        Release();

        CreateTempRT();
        Color[] Input = new Color[ImageWidth * ImageWidth];
        Color[] Output = new Color[ImageWidth * ImageWidth];
        for (int i = 0; i < Input.Length; i++)
        {
            Vector2Int Pos = new(i % ImageWidth, i / ImageWidth);
            Vector2 UV = new Vector2(Pos.x, Pos.y) / ImageWidth;
            float DistSqr = Mathf.Pow(0.5f - UV.x, 2) + Mathf.Pow(0.5f - UV.y, 2);
            Input[i] = DistSqr < 0.01f ? new Color(UV.x, UV.y, 1, 1) : new Color(0, 0, 0, 1);
        }

        float AbsMaxDistance = 0;
        float AbsMinDistance = 928375925f;

        Vector2Int[] FloodFillDirs =
        {
            new Vector2Int(-1, +1), new Vector2Int(+0, +1), new Vector2Int(+1, +1),
            new Vector2Int(-1, +0), new Vector2Int(+0, +0), new Vector2Int(+1, +0),
            new Vector2Int(-1, -1), new Vector2Int(+0, -1), new Vector2Int(+1, -1),
        };
        int StepSize = ImageWidth;

        int MaxStep = (int)Mathf.Log(ImageWidth, 2);
        for (int Step = 0; Step < MaxStep; Step++)
        {
            StepSize /= 2;
            Color[] TempInput = Step % 2 == 0 ? Input : Output;
            Color[] TempOutput = Step % 2 == 0 ? Output : Input;
            for (int Pixel = 0; Pixel < Input.Length; Pixel++)
            {
                float MinDis = Mathf.Pow(ImageWidth + 1, 2);
                Vector2Int ClosestWater = new Vector2Int(0, 0);
                bool bFoundWater = false;
                // check all the neighbours if they have water, then get the distance to the closest water position
                // -> this is the new water location we save
                for (int i = 0; i < FloodFillDirs.Length; i++)
                {
                    Vector2Int ID = new(Pixel % ImageWidth, Pixel / ImageWidth);
                    Vector2Int TargetID = ID + FloodFillDirs[i] * StepSize;
                    if (TargetID.x < 0 || TargetID.x >= ImageWidth || TargetID.y < 0 || TargetID.y >= ImageWidth)
                        continue;

                    int TargetPos = TargetID.y * ImageWidth + TargetID.x;
                    Color TargetValue = TempInput[TargetPos];

                    if (TargetValue.b <= 0)
                        continue;

                    Vector2Int WaterID = new Vector2Int((int)(TargetValue.r * ImageWidth), (int)(TargetValue.g * ImageWidth));

                    float Distance = Vector2.Distance(WaterID, ID);
                    if (Distance >= MinDis)
                        continue;

                    bFoundWater = true;
                    ClosestWater = WaterID;
                    MinDis = Distance;
                }

                Vector2 ClosestPos = StepSize > 1 ? ClosestWater : Vector2.zero;
                float FinalDistance = bFoundWater ? MinDis : 0;

                TempOutput[Pixel] = new Color(ClosestPos.x / ImageWidth, ClosestPos.y / ImageWidth, FinalDistance / ImageWidth, 1);


                if (TempOutput[Pixel].b > AbsMaxDistance)
                    AbsMaxDistance = TempOutput[Pixel].b;


                if (TempOutput[Pixel].b < AbsMinDistance)
                    AbsMinDistance = TempOutput[Pixel].b;
            }
        }

        Color[] FinalOutput = (MaxStep - 1) % 2 == 0 ? Output : Input;

        for (int i = 0; i < FinalOutput.Length; i++)
        {
            FinalOutput[i] = new Color(0, 0, 1 - FinalOutput[i].b * 5, 1);
        }

        Texture2D InputTex = new Texture2D(InputRT.width, InputRT.height, TextureFormat.RGBA32, false);
        InputTex.SetPixels(Input);
        InputTex.Apply();
        Graphics.CopyTexture(InputTex, InputRT);

        Texture2D OutputTex = new Texture2D(OutputRT.width, OutputRT.height, TextureFormat.RGBA32, false);
        OutputTex.SetPixels(Output);
        OutputTex.Apply();
        Graphics.CopyTexture(OutputTex, OutputRT);
    }
}
