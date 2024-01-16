using System.Collections;
using System.Collections.Generic;
using System.Runtime.Remoting.Lifetime;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;
using static HexagonConfig;

public class WorldGenerator : GameService
{

    struct BiomeMap
    {
        public Vector2 Range;
        public uint BiomeIndex;
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
        Tile EmptyTile = GetTileFromMapValue(new Vector2(0.1f, 0.6f), this);
        System.Array.Fill(LandData, EmptyTile);
        return LandData;
    }

    public Tile[] NoiseLand() {
        if (!IsInit)
            return new Tile[0];

        SetDataMap(NoiseLandKernel);

        EvenRT = new RenderTexture(ImageWidth, ImageWidth, 0, RenderTextureFormat.ARGB32);
        EvenRT.enableRandomWrite = true;
        EvenRT.Create();
        EvenRT.filterMode = FilterMode.Trilinear;
        EvenRT.wrapMode = TextureWrapMode.Repeat;

        MapShader.SetTexture(NoiseLandKernel, "Image", EvenRT);

        MapShader.Dispatch(NoiseLandKernel, GroupCount, GroupCount, 1);
        Vector2[] ImageData = new Vector2[ImageWidth * ImageWidth];
        MapValuesBuffer.GetData(ImageData);

        // convert image data (x: height, y: temp) to actual type
        // aspect ratio changes!
        float SizeMultiplier = (float)ImageWidth / MapWidth;
        Tile[] MapData = new Tile[MapWidth * MapWidth];
        for (int i = 0; i < MapData.Length; i++)
        {
            Vector2Int MapPos = new(i % MapWidth, i / MapWidth);
            Vector2Int ImagePos = new((int)(MapPos.x * SizeMultiplier), (int)(MapPos.y * SizeMultiplier));
            int ImageIndex = ImagePos.y * ImageWidth + ImagePos.x;
            Vector2 Value = ImageData[ImageIndex];
            MapData[i] = GetTileFromMapValue(Value, this);
        }

        return MapData;
    }

    private void OnDestroy() {
        Release();
    }

    private void Release() {
        if (EvenRT != null)
            EvenRT.Release();
        if (MapValuesBuffer != null)
            MapValuesBuffer.Release();
        if (TemperatureBuffer != null) 
           TemperatureBuffer.Release();
        if (HeightBuffer != null) 
            HeightBuffer.Release();
    }

    private void Init() {
        Release();

        InitMap();
        FillBuffersMap();
    }

    private void InitMap() {
        NoiseLandKernel = MapShader.FindKernel("NoiseLand");
        MapValuesBuffer = new ComputeBuffer(ImageWidth * ImageWidth, sizeof(float) * 2);
        TemperatureBuffer = new ComputeBuffer(TemperatureMap.Count, sizeof(float) * 2 + sizeof(uint));
        HeightBuffer = new ComputeBuffer(HeightOverrideMap.Count, sizeof(float) * 2 + sizeof(uint));
    }

    private void SetDataMap(int Kernel) {
        MapShader.SetInt("GroupCount", GroupCount);
        MapShader.SetInt("Width", ImageWidth);
        MapShader.SetBuffer(Kernel, "Output", MapValuesBuffer);
        MapShader.SetFloat("Seed", Seed);
        MapShader.SetFloat("Scale", Scale);
        MapShader.SetFloat("Factor", Factor);
        MapShader.SetFloat("Offset", Offset);
        MapShader.SetFloat("Amount", Amount);

        MapShader.SetInt("TemperatureCount", TemperatureMap.Count);
        MapShader.SetInt("HeightCount", HeightOverrideMap.Count);
        MapShader.SetBuffer(Kernel, "TemperatureMap", TemperatureBuffer);
        MapShader.SetBuffer(Kernel, "HeightMap", HeightBuffer);
        MapShader.SetTexture(Kernel, "BiomeColors", BiomeColors);

        List<BiomeMap> Temperatures = new();
        List<BiomeMap> Heights = new();
        foreach (var Tuple in TemperatureMap)
        {
            Temperatures.Add(new BiomeMap()
            {
                Range = new Vector2(Tuple.Key.Min, Tuple.Key.Max),
                BiomeIndex = (uint)MaskToInt((int)Tuple.Value, 16)
            });
        }
        foreach (var Tuple in HeightOverrideMap)
        {
            Heights.Add(new BiomeMap()
            {
                Range = new Vector2(Tuple.Key.Min, Tuple.Key.Max),
                BiomeIndex = (uint)MaskToInt((int)Tuple.Value, 16)
            });
        }
        TemperatureBuffer.SetData(Temperatures);
        HeightBuffer.SetData(Heights);
    }

    private void FillBuffersMap() {
        Vector2[] Map = new Vector2[ImageWidth * ImageWidth];
        MapValuesBuffer.SetData(Map);
    }

    public bool TryGetHexagonTypeForTemperature(float Temperature, out HexagonType Type)
    {
        return TryGetHexagonTypeForValue(Temperature, TemperatureMap, out Type);
    }

    public bool TryGetHexagonTypeOverrideForHeight(float Height, out HexagonType Type)
    {
        return TryGetHexagonTypeForValue(Height, HeightOverrideMap, out Type);
    }

    public bool TryGetHexagonHeightForHeight(float Value, out HexagonHeight Height)
    {
        return TryGetHexagonTypeForValue(Value, HeightMap, out Height);
    }

    private bool TryGetHexagonTypeForValue<T>(float Value, SerializedDictionary<FloatRange, T> Dictionary, out T Type)
    {
        foreach (var Tuple in Dictionary.Tuples)
        {
            if (Tuple.Key.Contains(Value))
            {
                Type = Tuple.Value;
                return true;
            }
        }

        Type = default;
        return false;
    }

    public bool TryGetTemperatureFromHexagonType(HexagonType Type, out float Temperature)
    {
        return TryGetEntryForHexagonType(Type, TemperatureMap, out Temperature);
    }

    public bool TryGetHeightOverrideFromHexagonType(HexagonType Type, out float HeightOverride)
    {
        return TryGetEntryForHexagonType(Type, HeightOverrideMap, out HeightOverride);
    }

    public bool TryGetHeightFromHexagonHeight(HexagonHeight HexHeight, out float Height)
    {
        return TryGetEntryForHexagonHeight(HexHeight, HeightMap, out Height);
    }

    private bool TryGetEntryForHexagonType(HexagonType Type, SerializedDictionary<FloatRange, HexagonType> Dictionary, out float Entry)
    {
        foreach (var Tuple in Dictionary.Tuples)
        {
            if (Tuple.Value.HasFlag(Type))
            {
                Entry = Tuple.Key.GetMidPoint();
                return true;
            }
        }

        Entry = -1;
        return false;
    }

    private bool TryGetEntryForHexagonHeight(HexagonHeight Type, SerializedDictionary<FloatRange, HexagonHeight> Dictionary, out float Entry)
    {
        foreach (var Tuple in Dictionary.Tuples)
        {
            if (Tuple.Value == Type)
            {
                Entry = Tuple.Key.GetMidPoint();
                return true;
            }
        }

        Entry = -1;
        return false;
    }

    public SerializedDictionary<FloatRange, HexagonType> HeightOverrideMap = new();
    public SerializedDictionary<FloatRange, HexagonHeight> HeightMap = new();
    public SerializedDictionary<FloatRange, HexagonType> TemperatureMap = new();

    public ComputeShader MapShader;
    public RenderTexture EvenRT;
    public Texture2D BiomeColors;

    // how many iterations of noise
    public float Amount = 1;
    // how zoomed-in the noise should be
    public float Scale = 1;
    // how strong the noise should be
    public float Factor = 1;
    public float Offset = 0;
    public float Seed = 0;

    private int NoiseLandKernel;
    private ComputeBuffer MapValuesBuffer;
    private ComputeBuffer TemperatureBuffer;
    private ComputeBuffer HeightBuffer;

    // to make compute calculations easier, make sure that GroupCount * NumThreads = RT.width!
    private static int ImageWidth = 256;
    private static int GroupCount = ImageWidth / 16;

}
