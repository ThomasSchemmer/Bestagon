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

    private Vector2[] EmptyLand()
    {
        if (MapValuesBuffer == null)
        {
            Init();
        }
        Vector2[] LandData = new Vector2[ImageWidth * ImageWidth];
        // set to water at meadow temperature
        System.Array.Fill(LandData, new Vector2(0.1f, 0.6f));
        return LandData;
    }

    private Vector2[] NoiseLand() {
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
        Vector2[] LandData = new Vector2[ImageWidth * ImageWidth];
        MapValuesBuffer.GetData(LandData);

        return LandData;
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
}
