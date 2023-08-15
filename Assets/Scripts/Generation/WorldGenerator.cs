using System.Collections;
using System.Collections.Generic;
using System.Runtime.Remoting.Lifetime;
using UnityEngine;

public class WorldGenerator : MonoBehaviour
{
    private void Start() {
        Instance = this;    
    }

    public void CreatePlates() {
        Init(false);

        SetDataPlates(CreatePlatesKernel);

        PlateShader.Dispatch(CreatePlatesKernel, GroupCount, GroupCount, 1);
    }

    public void Move() {
        for (int i = 1; i < NumCenters; i++) {
            Move(1 << i);
        }
    }

    public void Move(int Index) {
        if (EvenRT == null) {
            Init(false);
        }

        SetDataPlates(MovePlatesKernel);
        PlateShader.SetInt("CurrentIndex", Index);

        PlateShader.Dispatch(MovePlatesKernel, GroupCount, GroupCount, 1);
        CopyBuffers();
    }

    public void CopyBuffers() {
        SetDataPlates(CopyBuffersKernel);
        PlateShader.Dispatch(CopyBuffersKernel, GroupCount, GroupCount, 1);
    }

    public Vector2[] NoiseLand() {
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
        if (ColorsBuffer != null)
            ColorsBuffer.Release();
        if (CentersBuffer != null)
            CentersBuffer.Release();
        if (EvenIndicesBuffer != null)
            EvenIndicesBuffer.Release();
        if (OddIndicesBuffer != null)
            OddIndicesBuffer.Release();
        if (MapValuesBuffer != null)
            MapValuesBuffer.Release();
    }

    private void Init(bool bIsCreatingMap = true) {
        Release();

        if (bIsCreatingMap) {
            InitMap();
            FillBuffersMap();
        } else {
            InitPlates();
            FillBuffersPlates();
        }
    }

    private void InitMap() {
        NoiseLandKernel = MapShader.FindKernel("NoiseLand");
        MapValuesBuffer = new ComputeBuffer(ImageWidth * ImageWidth, sizeof(float) * 2);
    }

    private void InitPlates() {
        EvenRT = new RenderTexture(ImageWidth, ImageWidth, 0, RenderTextureFormat.ARGB32);
        EvenRT.enableRandomWrite = true;
        EvenRT.Create();
        EvenRT.filterMode = FilterMode.Trilinear;
        EvenRT.wrapMode = TextureWrapMode.Repeat;
        OddRT = new RenderTexture(ImageWidth, ImageWidth, 0, RenderTextureFormat.ARGB32);
        OddRT.enableRandomWrite = true;
        OddRT.Create();
        OddRT.filterMode = FilterMode.Trilinear;
        OddRT.wrapMode = TextureWrapMode.Repeat;

        CreatePlatesKernel = PlateShader.FindKernel("CreatePlates");
        MovePlatesKernel = PlateShader.FindKernel("MovePlates");
        CopyBuffersKernel = PlateShader.FindKernel("CopyBuffers");

        ColorsBuffer = new ComputeBuffer(NumCenters, 4 * sizeof(float));
        CentersBuffer = new ComputeBuffer(NumCenters, 2 * sizeof(float));
        EvenIndicesBuffer = new ComputeBuffer(ImageWidth * ImageWidth, sizeof(int));
        OddIndicesBuffer = new ComputeBuffer(ImageWidth * ImageWidth, sizeof(int));
    }

    private void SetDataPlates(int Kernel) {
        PlateShader.SetBuffer(Kernel, "Colors", ColorsBuffer);
        PlateShader.SetBuffer(Kernel, "Centers", CentersBuffer);
        PlateShader.SetBuffer(Kernel, "EvenIndices", EvenIndicesBuffer);
        PlateShader.SetBuffer(Kernel, "OddIndices", OddIndicesBuffer);
        PlateShader.SetTexture(Kernel, "EvenResult", EvenRT);
        PlateShader.SetTexture(Kernel, "OddResult", OddRT);
        PlateShader.SetInt("CentersCount", NumCenters);
        PlateShader.SetInt("GroupCount", GroupCount);
        PlateShader.SetInt("Width", EvenRT.width);
    }

    private void SetDataMap(int Kernel) {
        MapShader.SetInt("GroupCount", GroupCount);
        MapShader.SetInt("Width", ImageWidth);
        MapShader.SetBuffer(Kernel, "Values", MapValuesBuffer);
        MapShader.SetVector("Seed", new Vector4(15, 0, 0, 0));
    }

    private void FillBuffersPlates() {
        //Random.InitState(0);
        List<Vector2> Centers = new();
        // add "invalid" location at index 0
        Centers.Add(new Vector2(-100, -100));
        for (int i = 1; i < NumCenters; i++) {
            Centers.Add(new Vector2(Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f)));
        }
        CentersBuffer.SetData(Centers.ToArray());

        List<Vector4> Colors = new();
        // add "invalid" color at index 0
        Colors.Add(new Color(0, 0, 0));
        for (int i = 1; i < NumCenters; i++) {
            Color Color = Random.ColorHSV();
            Colors.Add(new Vector4(Color.r, Color.g, Color.b, 1));
        }
        ColorsBuffer.SetData(Colors.ToArray());
    }

    private void FillBuffersMap() {
        Vector2[] Map = new Vector2[ImageWidth * ImageWidth];
        MapValuesBuffer.SetData(Map);
    }

    public ComputeShader MapShader, PlateShader;
    public RenderTexture EvenRT, OddRT;

    private int CreatePlatesKernel;
    private int MovePlatesKernel;
    private int CopyBuffersKernel;
    private int NoiseLandKernel;
    private ComputeBuffer ColorsBuffer;
    private ComputeBuffer CentersBuffer;
    private ComputeBuffer EvenIndicesBuffer;
    private ComputeBuffer OddIndicesBuffer;
    private ComputeBuffer MapValuesBuffer;

    // to make compute calculations easier, make sure that GroupCount * NumThreads = RT.width!
    private static int ImageWidth = 256;
    private static int GroupCount = ImageWidth / 16;
    private static int NumCenters = 7;

    public static WorldGenerator Instance;
}
