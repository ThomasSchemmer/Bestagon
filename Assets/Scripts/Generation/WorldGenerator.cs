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
        public uint DecorationIndex;

        public HexagonInfo(float a, float b, float c, uint d, uint e, uint f)
        {
            Height = a;
            Temperature = b;
            Humidity = c;
            TypeIndex = d;
            HexHeightIndex = e;
            DecorationIndex = f;
        }

        public static int GetSize()
        {
            return sizeof(float) * 3 + sizeof(uint) * 3;
        }
    }

    protected override void StartServiceInternal() {
        Init();
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
            HexagonData EmptyTile = HexagonData.CreateFromInfo(new HexagonInfo(0.1f, 0.6f, 0f, 3, 0, 0));
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
        if (DecorationsBuffer != null)
            DecorationsBuffer.Release();
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

        HexagonInfoBuffer = new ComputeBuffer(ImageWidth * ImageWidth, HexagonInfo.GetSize());
        ClimateBuffer = new ComputeBuffer(BiomeMap.ClimateMap.Count, sizeof(float) * 4 + sizeof(uint));
        HeightOverrideBuffer = new ComputeBuffer(BiomeMap.HeightOverrideMap.Count, sizeof(float) * 2 + sizeof(uint));
        HeightBuffer = new ComputeBuffer(BiomeMap.HeightMap.Count, sizeof(float) * 2 + sizeof(int));
        DecorationsBuffer = new ComputeBuffer(BiomeMap.DecorationsMap.Count, sizeof(float) * 2 + sizeof(int));
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
        MapShader.SetInt("DecorationsCount", BiomeMap.DecorationsMap.Count);
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
        MapShader.SetBuffer(Kernel, "DecorationsMap", DecorationsBuffer);
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
        List<RangeStruct> Decorations = new();
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
        foreach (var Tuple in BiomeMap.DecorationsMap)
        {
            Decorations.Add(new()
            {
                Range = new Vector2(Tuple.Key.Min, Tuple.Key.Max),
                Index = (uint)Tuple.Value   
            });
        }

        ClimateBuffer.SetData(Climates);
        HeightOverrideBuffer.SetData(HeightOverrides);
        HeightBuffer.SetData(Heights);
        HexagonInfoBuffer.SetData(Map);
        DecorationsBuffer.SetData(Decorations);

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
    private ComputeBuffer DecorationsBuffer;
    private ComputeBuffer HistogramInputBuffer;
    private ComputeBuffer HistogramResultBuffer;

    // to make compute calculations easier, make sure that GroupCount * NumThreads = RT.width!
    private static int ImageWidth = 256;
    private static int GroupCount = ImageWidth / 16;
    private static int HistogramResolution = 256;

}
