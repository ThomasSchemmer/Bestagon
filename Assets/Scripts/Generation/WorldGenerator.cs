using System.Collections;
using System.Collections.Generic;
using System.Runtime.Remoting.Lifetime;
using UnityEngine;

public class WorldGenerator : MonoBehaviour
{
    public void Execute() {
        Init();
        CurrentRT = 0;

        SetData(CreatePlatesKernel);

        ComputeShader.Dispatch(CreatePlatesKernel, GroupCount, GroupCount, 1);
    }

    public void Move(int Amount) {
        if (EvenRT == null) {
            Init();
        }
        CurrentRT++;

        SetData(MovePlatesKernel);

        ComputeShader.Dispatch(MovePlatesKernel, GroupCount, GroupCount, 1);
    }

    private void FillBuffers() {
        Random.InitState(0);
        List<Vector2> Centers = new();
        for (int i = 0; i < NumCenters; i++) {
            Centers.Add(new Vector2(Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f)));
        }
        CentersBuffer.SetData(Centers.ToArray());


        List<Vector2> Directions = new();
        int Range = 1;
        for (int i = 0; i < NumCenters; i++) {
            Vector2 Direction = new Vector2(Random.Range(-Range, Range), Random.Range(-Range, Range));
            Direction.Normalize();
            Directions.Add(Direction);
        }
        DirectionsBuffer.SetData(Directions.ToArray());

        List<Vector4> Colors = new();
        for (int i = 0; i < NumCenters; i++) {
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
        if (DirectionsBuffer != null)
            DirectionsBuffer.Release();
        if (EvenIndicesBuffer != null)
            EvenIndicesBuffer.Release();
        if (OddIndicesBuffer != null)
            OddIndicesBuffer.Release();
    }

    private void Init() {
        Release();

        EvenRT = new RenderTexture(256, 256, 0, RenderTextureFormat.ARGB32);
        EvenRT.enableRandomWrite = true;
        EvenRT.Create();
        EvenRT.filterMode = FilterMode.Trilinear;
        EvenRT.wrapMode = TextureWrapMode.Repeat; 
        OddRT = new RenderTexture(256, 256, 0, RenderTextureFormat.ARGB32);
        OddRT.enableRandomWrite = true;
        OddRT.Create();
        OddRT.filterMode = FilterMode.Trilinear;
        OddRT.wrapMode = TextureWrapMode.Repeat;

        CreatePlatesKernel = ComputeShader.FindKernel("CreatePlates");
        MovePlatesKernel = ComputeShader.FindKernel("MovePlates");

        ColorsBuffer = new ComputeBuffer(NumCenters, 4 * sizeof(float));
        CentersBuffer = new ComputeBuffer(NumCenters, 2 * sizeof(float));
        DirectionsBuffer = new ComputeBuffer(NumCenters, 2 * sizeof(float));
        EvenIndicesBuffer = new ComputeBuffer(256 * 256, sizeof(int));
        OddIndicesBuffer = new ComputeBuffer(256 * 256, sizeof(int));

        FillBuffers();
    }

    private void SetData(int Kernel) {
        ComputeShader.SetBuffer(Kernel, "Colors", ColorsBuffer);
        ComputeShader.SetBuffer(Kernel, "Centers", CentersBuffer);
        ComputeShader.SetBuffer(Kernel, "Directions", DirectionsBuffer);
        ComputeShader.SetBuffer(Kernel, "EvenIndices", EvenIndicesBuffer);
        ComputeShader.SetBuffer(Kernel, "OddIndices", OddIndicesBuffer);
        ComputeShader.SetInt("CentersCount", NumCenters);
        ComputeShader.SetInt("GroupCount", GroupCount);
        ComputeShader.SetInt("Width", EvenRT.width);
        ComputeShader.SetTexture(Kernel, "EvenResult", EvenRT);
        ComputeShader.SetTexture(Kernel, "OddResult", OddRT);
    }

    public ComputeShader ComputeShader;
    public RenderTexture EvenRT, OddRT;

    private int CreatePlatesKernel;
    private int MovePlatesKernel;
    private ComputeBuffer ColorsBuffer;
    private ComputeBuffer CentersBuffer;
    private ComputeBuffer DirectionsBuffer;
    private ComputeBuffer EvenIndicesBuffer;
    private ComputeBuffer OddIndicesBuffer;

    private static int NumCenters = 15;
    private static int GroupCount = 16;
    private uint CurrentRT = 0;

}
