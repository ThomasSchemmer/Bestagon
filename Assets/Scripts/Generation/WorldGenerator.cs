using System.Collections;
using System.Collections.Generic;
using System.Runtime.Remoting.Lifetime;
using UnityEngine;

public class WorldGenerator : MonoBehaviour
{
    public void Execute() {
        Init();

        SetData(CreatePlatesKernel);

        ComputeShader.Dispatch(CreatePlatesKernel, GroupCount, GroupCount, 1);
    }

    public void Move() {
        for (int i = 1; i < NumCenters; i++) {
            Move(1 << i);
        }
    }

    public void Move(int Index) {
        if (EvenRT == null) {
            Init();
        }

        SetData(MovePlatesKernel);
        ComputeShader.SetInt("CurrentIndex", Index);

        ComputeShader.Dispatch(MovePlatesKernel, GroupCount, GroupCount, 1);
        CopyBuffers();
    }

    public void CopyBuffers() {
        SetData(CopyBuffersKernel);
        ComputeShader.Dispatch(CopyBuffersKernel, GroupCount, GroupCount, 1);
    }

    private void FillBuffers() {
        Random.InitState(0);
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
    }

    private void Init() {
        Release();

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

        CreatePlatesKernel = ComputeShader.FindKernel("CreatePlates");
        MovePlatesKernel = ComputeShader.FindKernel("MovePlates");
        CopyBuffersKernel = ComputeShader.FindKernel("CopyBuffers");

        ColorsBuffer = new ComputeBuffer(NumCenters, 4 * sizeof(float));
        CentersBuffer = new ComputeBuffer(NumCenters, 2 * sizeof(float));
        EvenIndicesBuffer = new ComputeBuffer(ImageWidth * ImageWidth, sizeof(int));
        OddIndicesBuffer = new ComputeBuffer(ImageWidth * ImageWidth, sizeof(int));

        FillBuffers();
    }

    private void SetData(int Kernel) {
        ComputeShader.SetBuffer(Kernel, "Colors", ColorsBuffer);
        ComputeShader.SetBuffer(Kernel, "Centers", CentersBuffer);
        ComputeShader.SetBuffer(Kernel, "EvenIndices", EvenIndicesBuffer);
        ComputeShader.SetBuffer(Kernel, "OddIndices", OddIndicesBuffer);
        ComputeShader.SetTexture(Kernel, "EvenResult", EvenRT);
        ComputeShader.SetTexture(Kernel, "OddResult", OddRT);
        ComputeShader.SetInt("CentersCount", NumCenters);
        ComputeShader.SetInt("GroupCount", GroupCount);
        ComputeShader.SetInt("Width", EvenRT.width);
    }

    public ComputeShader ComputeShader;
    public RenderTexture EvenRT, OddRT;

    private int CreatePlatesKernel;
    private int MovePlatesKernel;
    private int CopyBuffersKernel;
    private ComputeBuffer ColorsBuffer;
    private ComputeBuffer CentersBuffer;
    private ComputeBuffer EvenIndicesBuffer;
    private ComputeBuffer OddIndicesBuffer;

    // to make compute calculations easier, make sure that GroupCount * NumThreads = RT.width!
    private static int ImageWidth = 256;
    private static int GroupCount = ImageWidth / 16;
    private static int NumCenters = 4;
}
