using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.AI;

public class VoronoiTest : MonoBehaviour
{
    public ComputeShader VoronoiCompute;
    public RenderTexture TargetTexture;
    [Range(0.25f, 5)]
    public float Zoom;
    [Range(0, 31)]
    public int Slice;
    [Range(1, 4)]
    public int Iterations;
    [Range(0, 5)]
    public float Factor;
    public int CellCount;
    [Range(0, 4)]
    public int Selection;

    private ComputeBuffer VoronoiBuffer;
    private int Kernel;

    private struct CloudPoint
    {
        // stores 4 * 8bit of noise data in range of 0..255
        public int Data;
    }
    
    void Start()
    {
        VoronoiBuffer = new(256 * 256 * 32, sizeof(int));

        Kernel = VoronoiCompute.FindKernel("Main");
        VoronoiCompute.SetBuffer(Kernel, "Result", VoronoiBuffer);

    }
    void Update()
    {

        VoronoiCompute.SetFloat("Zoom", Zoom);
        VoronoiCompute.SetFloat("CellCount", CellCount);
        VoronoiCompute.SetFloat("Iterations", Iterations);
        VoronoiCompute.SetFloat("Factor", Factor);
        VoronoiCompute.Dispatch(Kernel, 32, 32, 32);

        CloudPoint[] Data = new CloudPoint[VoronoiBuffer.count];
        VoronoiBuffer.GetData(Data);

        Texture2D VoronoiTexture = new Texture2D(256, 256);
        VoronoiTexture.SetPixels(GetPixelFromData(Data, Slice));
        VoronoiTexture.Apply();
        Graphics.Blit(VoronoiTexture, TargetTexture);
        EditorUtility.SetDirty(TargetTexture);
    }

    private void OnDestroy()
    {
        VoronoiBuffer?.Dispose();
    }

    private Color[] Convert(CloudPoint[] Data, int Slice)
    {
        Color[] Colors = new Color[256 * 256];
        int Start = Slice * 256 * 256;
        for (int i = 0; i < Colors.Length; i++)
        {
            int Offset = (3 - Selection) * 8;
            int Value = (Data[i + Start].Data >> Offset) & 0xFF;
            float a = Value / 255.0f;
            Colors[i] = new Color(a, a, a);
        }
        return Colors;
    }

    float Map(float v, float minOld, float maxOld, float minNew, float maxNew)
    {
        return minNew + ((v - minOld) * (maxNew - minNew)) / (maxOld - minOld);
    }

    private Color[] GetPixelFromData(CloudPoint[] Data, int Slice)
    {
        Color[] Converted = Convert(Data, Slice);
        if (Selection < 4)
            return Converted;

        for (int i = 0; i < Converted.Length; i++)
        {
            Color Current = Converted[i];
            // todo: why is this weird?
            float Value = Current.g * 0.625f + Current.b * 0.25f + Current.a * 0.125f;
            float alpha = Map(Current.r, Value, 1, 0, 1);
            Converted[i] = new Color(Value, Value, Value);
        }
        return Converted;
    }



}
