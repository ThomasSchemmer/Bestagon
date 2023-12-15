using System.Collections;
using System.Collections.Generic;
using System.Runtime.Remoting.Lifetime;
using UnityEngine;

public class WorldGenerator : GameService
{
    protected override void StartServiceInternal() {
        HexagonConfig.MapData = Game.Instance.Mode == Game.GameMode.Game ? NoiseLand() : EmptyLand();
    }

    protected override void StopServiceInternal() { }

    private HexagonConfig.Tile[] EmptyLand()
    {
        if (MapValuesBuffer == null)
        {
            Init();
        }
        HexagonConfig.Tile[] LandData = new HexagonConfig.Tile[HexagonConfig.MapWidth * HexagonConfig.MapWidth];
        // set to water at meadow temperature
        HexagonConfig.Tile EmptyTile = HexagonConfig.GetTileFromMapValue(new Vector2(0.1f, 0.6f));
        System.Array.Fill(LandData, EmptyTile);
        return LandData;
    }

    private HexagonConfig.Tile[] NoiseLand() {
        if (MapValuesBuffer == null) {
            Init();
        }

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
        float SizeMultiplier = (float)ImageWidth / HexagonConfig.MapWidth;
        HexagonConfig.Tile[] MapData = new HexagonConfig.Tile[HexagonConfig.MapWidth * HexagonConfig.MapWidth];
        for (int i = 0; i < MapData.Length; i++)
        {
            Vector2Int MapPos = new(i % HexagonConfig.MapWidth, i / HexagonConfig.MapWidth);
            Vector2Int ImagePos = new((int)(MapPos.x * SizeMultiplier), (int)(MapPos.y * SizeMultiplier));
            int ImageIndex = ImagePos.y * ImageWidth + ImagePos.x;
            Vector2 Value = ImageData[ImageIndex];
            MapData[i] = new HexagonConfig.Tile(
                HexagonConfig.GetHeightFromMapValue(Value),
                HexagonConfig.GetTypeFromMapValue(Value)
            );
        }

        return MapData;
    }

    public void Save()
    {
        byte[] Bytes = HexagonConfig.MapToBinary();
        string Filename = SaveGamePath + "Save.map";
        System.IO.File.WriteAllBytes(Filename, Bytes);
    }

    public void Load()
    {
        string Filename = SaveGamePath + "Save.map";
        byte[] Bytes = System.IO.File.ReadAllBytes(Filename);
        HexagonConfig.LoadMap(Bytes);
        if (!Game.TryGetService(out MapGenerator Generator))
            return;

        Generator.GenerateMap();
    }

    private void OnDestroy() {
        Release();
    }

    private void Release() {
        if (EvenRT != null)
            EvenRT.Release();
        if (MapValuesBuffer != null)
            MapValuesBuffer.Release();
    }

    private void Init() {
        Release();

        InitMap();
        FillBuffersMap();
    }

    private void InitMap() {
        NoiseLandKernel = MapShader.FindKernel("NoiseLand");
        MapValuesBuffer = new ComputeBuffer(ImageWidth * ImageWidth, sizeof(float) * 2);
    }

    private void SetDataMap(int Kernel) {
        MapShader.SetInt("GroupCount", GroupCount);
        MapShader.SetInt("Width", ImageWidth);
        MapShader.SetBuffer(Kernel, "Values", MapValuesBuffer);
        MapShader.SetVector("Seed", new Vector4(15, 0, 0, 0));
    }

    private void FillBuffersMap() {
        Vector2[] Map = new Vector2[ImageWidth * ImageWidth];
        MapValuesBuffer.SetData(Map);
    }

    public ComputeShader MapShader;
    public RenderTexture EvenRT;

    private int NoiseLandKernel;
    private ComputeBuffer MapValuesBuffer;

    // to make compute calculations easier, make sure that GroupCount * NumThreads = RT.width!
    private static int ImageWidth = 256;
    private static int GroupCount = ImageWidth / 16;

    private static string SaveGamePath = Application.dataPath + "/Resources/Pictures/";
}
