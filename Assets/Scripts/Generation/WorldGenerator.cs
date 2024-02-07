using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
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
        public uint Index;
    }
    
    public struct HexagonInfo
    {
        public float Height;
        public float Temperature;
        public float Humidity;
        public uint TypeIndex;
        public uint HexHeightIndex;

        public HexagonInfo(float a, float b, float c, uint d, uint e)
        {
            Height = a;
            Temperature = b;
            Humidity = c;
            TypeIndex = d;
            HexHeightIndex = e;
        }
    }

    protected override void StartServiceInternal() {
        Init();
        IsInit = true;
        _OnInit?.Invoke();
    }

    protected override void StopServiceInternal() { }

    public HexagonData[] EmptyLand()
    {
        if (!IsInit)
            return new HexagonData[0];

        HexagonData[] LandData = new HexagonData[MapWidth * MapWidth];
        // set to water, cant use array.copy as it apparently doesnt clone
        for (int i = 0; i < LandData.Length; i++)
        {
            HexagonData EmptyTile = HexagonData.CreateFromInfo(new HexagonInfo(0.1f, 0.6f, 0f, 3, 0));
            EmptyTile.UpdateDiscoveryState(HexagonData.DiscoveryState.Visited);
            LandData[i] = EmptyTile;
        }    
        return LandData;
    }

    public HexagonData[] NoiseLand(bool bIncludeHumidity) {
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
        uint[] HistogramInput = new uint[HistogramInputBuffer.count];
        HistogramInputBuffer.GetData(HistogramInput);

        MapShader.Dispatch(HistogramNormalizationKernel, 3, 1, 1);
        HistogramInputBuffer.GetData(HistogramInput);

        MapShader.Dispatch(TypeKernel, GroupCount, GroupCount, 1);

        uint[] HistogramOutput = new uint[HistogramResultBuffer.count];
        HistogramResultBuffer.GetData(HistogramOutput);

        return GetMapData();
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
        if (HeightBuffer != null)
            HeightBuffer.Release();
        if (HistogramInputBuffer != null)
            HistogramInputBuffer.Release();
        if (HistogramResultBuffer != null)
            HistogramResultBuffer.Release();
    }

    private void Init() {
        Release();

        CreateTempRT();
        InitBuffers();

        SetDataGlobal();
        SetDataHeightTemperature();
        SetDataType();
        SetDataHistogram();

        FillBuffers();
    }

    private void InitBuffers() {
        HeightTemperatureKernel = MapShader.FindKernel("HeightTemperatureCalculation");
        JumpFloodKernel = MapShader.FindKernel("JumpFlood");
        TypeKernel = MapShader.FindKernel("TypeCalculation");
        HistogramNormalizationKernel = MapShader.FindKernel("HistogramNormalization");

        HexagonInfoBuffer = new ComputeBuffer(ImageWidth * ImageWidth, sizeof(float) * 3 + sizeof(uint) * 2);
        ClimateBuffer = new ComputeBuffer(BiomeMap.ClimateMap.Count, sizeof(float) * 4 + sizeof(uint));
        HeightOverrideBuffer = new ComputeBuffer(BiomeMap.HeightOverrideMap.Count, sizeof(float) * 2 + sizeof(uint));
        HeightBuffer = new ComputeBuffer(BiomeMap.HeightMap.Count, sizeof(float) * 2 + sizeof(int));
        HistogramInputBuffer = new ComputeBuffer(HistogramResolution * 3 + 3, sizeof(uint));
        HistogramResultBuffer = new ComputeBuffer(HistogramResolution * 3, sizeof(uint));
    }

    private void SetDataHeightTemperature() {
        MapShader.SetFloat("Seed", Seed);
        MapShader.SetFloat("Scale", Scale);
        MapShader.SetFloat("Factor", Factor);
        MapShader.SetFloat("Offset", Offset);
        MapShader.SetFloat("Amount", Amount);
        MapShader.SetFloat("WaterCutoff", GetWaterCutoff());

        SetBuffersForKernel(HeightTemperatureKernel);
    }

    private void SetDataJumpFlood(int StepSize, int StepCount)
    {
        MapShader.SetInt("StepSize", StepSize);

        bool IsEven = StepCount % 2 == 0;

        SetBuffersForKernel(JumpFloodKernel);
        MapShader.SetTexture(JumpFloodKernel, "InputImage", IsEven ? InputRT : OutputRT);
        MapShader.SetTexture(JumpFloodKernel, "OutputImage", IsEven ? OutputRT : InputRT);
    }

    private void SetDataType()
    {
        MapShader.SetInt("ClimateCount", BiomeMap.ClimateMap.Count);
        MapShader.SetInt("HeightCount", BiomeMap.HeightMap.Count);
        MapShader.SetInt("HeightOverrideCount", BiomeMap.HeightOverrideMap.Count);
        SetBuffersForKernel(TypeKernel);
    }

    private void SetDataGlobal()
    {
        MapShader.SetInt("GroupCount", GroupCount);
        MapShader.SetInt("Width", ImageWidth);
        MapShader.SetInt("HistogramResolution", HistogramResolution);
    }

    private void SetDataHistogram()
    {
        SetBuffersForKernel(HistogramNormalizationKernel);
    }

    private void SetBuffersForKernel(int Kernel)
    {
        MapShader.SetBuffer(Kernel, "ClimateMap", ClimateBuffer);
        MapShader.SetBuffer(Kernel, "HeightOverrideMap", HeightOverrideBuffer);
        MapShader.SetBuffer(Kernel, "HeightMap", HeightBuffer);
        MapShader.SetBuffer(Kernel, "HexagonInfos", HexagonInfoBuffer);
        MapShader.SetBuffer(Kernel, "HistogramInput", HistogramInputBuffer);
        MapShader.SetBuffer(Kernel, "HistogramResult", HistogramResultBuffer);

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
        List<RangeStruct> Heights = new();
        foreach (Biome Biome in BiomeMap.ClimateMap)
        {
            Climates.Add(new BiomeStruct()
            {
                Position = Biome.Rect.position,
                Size = Biome.Rect.size,
                BiomeIndex = (uint)Biome.HexagonType
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
                Index = (uint)OverrideType
            });
        }
        foreach (var Tuple in BiomeMap.HeightMap)
        {
            Heights.Add(new()
            {
                Range = new Vector2(Tuple.Key.Min, Tuple.Key.Max),
                Index = (uint)Tuple.Value
            });
        }

        ClimateBuffer.SetData(Climates);
        HeightOverrideBuffer.SetData(HeightOverrides);
        HeightBuffer.SetData(Heights);
        HexagonInfoBuffer.SetData(Map);

        // write a uint max for each CdfMin
        int Max = -1;
        uint[] HistogramData = new uint[HistogramInputBuffer.count];
        HistogramData[HistogramData.Length - 3] = (uint)Max;
        HistogramData[HistogramData.Length - 2] = (uint)Max;
        HistogramData[HistogramData.Length - 1] = (uint)Max;
        HistogramInputBuffer.SetData(HistogramData);
    }

    private float GetWaterCutoff()
    {
        foreach (var Tuple in BiomeMap.HeightMap)
        {
            if (Tuple.Value == HexagonHeight.Sea)
                return Tuple.Key.Max;
        }
        return 0.1f;
    }

    private HexagonData[] GetMapData()
    {
        HexagonInfo[] ImageData = new HexagonInfo[ImageWidth * ImageWidth];
        HexagonInfoBuffer.GetData(ImageData);

        BiomeStruct[] Biomes = new BiomeStruct[BiomeMap.ClimateMap.Count];
        ClimateBuffer.GetData(Biomes);

        // convert shader data to actual map
        // size changes!
        float SizeMultiplier = (float)ImageWidth / MapWidth;
        HexagonData[] MapData = new HexagonData[MapWidth * MapWidth];
        for (int i = 0; i < MapData.Length; i++)
        {
            Vector2Int MapPos = new(i % MapWidth, i / MapWidth);
            if (MapPos.x == 14 && MapPos.y == 31)
            {
                Debug.Log("");
            }
            Vector2Int ImagePos = new((int)(MapPos.x * SizeMultiplier), (int)(MapPos.y * SizeMultiplier));
            int ImageIndex = ImagePos.y * ImageWidth + ImagePos.x;
            MapData[i] = HexagonData.CreateFromInfo(ImageData[ImageIndex]);
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

    private int HeightTemperatureKernel, JumpFloodKernel, TypeKernel, HistogramNormalizationKernel;
    private ComputeBuffer HexagonInfoBuffer;
    private ComputeBuffer ClimateBuffer;
    private ComputeBuffer HeightOverrideBuffer;
    private ComputeBuffer HeightBuffer;
    private ComputeBuffer HistogramInputBuffer;
    private ComputeBuffer HistogramResultBuffer;

    // to make compute calculations easier, make sure that GroupCount * NumThreads = RT.width!
    private static int ImageWidth = 256;
    private static int GroupCount = ImageWidth / 16;
    private static int HistogramResolution = 256;


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
