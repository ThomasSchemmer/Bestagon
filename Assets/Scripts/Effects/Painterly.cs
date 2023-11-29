using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.Rendering;

[RequireComponent(typeof(MeshFilter))]
public class Painterly : MonoBehaviour
{
    private struct MeshProperties
    {
        public Vector3 position;
        public Vector4 quat;
        public Vector4 color;

        public static int Size()
        {
            return 
                sizeof(float) * 3 +     // position
                sizeof(float) * 4 +     // quat
                sizeof(float) * 4;      // color
        }
    }

    public Texture Texture;
    public Material Mat;
    public ComputeShader ComputeShader;
    public Mesh Mesh;

    private ComputeBuffer VertexBuffer;
    private ComputeBuffer TriangleBuffer;
    private ComputeBuffer PropertiesBuffer;
    private ComputeBuffer ArgsBuffer;

    private int ComputePositionsKernel;

    public void Start()
    {
        FillShader();
    }

    private void Update()
    {
        Graphics.DrawMeshInstancedIndirect(Mesh, 0, Mat, new Bounds(transform.position, Vector3.one * 10), ArgsBuffer);
    }

    private void FillShader()
    {
        Mesh OriginalMesh = GetComponent<MeshFilter>().sharedMesh;
        int VertexCount = OriginalMesh.vertices.Length;
        int TriangleCount = OriginalMesh.triangles.Length;
        int BrushCount = 1;//TriangleCount / 3;
        Vector3 GlobalNormal = new Vector3(0, 0, 1);

        // Arguments for drawing mesh.
        // 0 == number of triangle indices, 1 == population, others are only relevant if drawing submeshes.
        uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
        args[0] = (uint)Mesh.GetIndexCount(0);
        args[1] = (uint)BrushCount;
        args[2] = (uint)Mesh.GetIndexStart(0);
        args[3] = (uint)Mesh.GetBaseVertex(0);
        ArgsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        ArgsBuffer.SetData(args);

        ComputePositionsKernel = ComputeShader.FindKernel("ComputePositions");

        VertexBuffer = new ComputeBuffer(VertexCount, sizeof(float) * 3);
        VertexBuffer.SetData(OriginalMesh.vertices);
        TriangleBuffer = new ComputeBuffer(TriangleCount, sizeof(int));
        TriangleBuffer.SetData(OriginalMesh.triangles);

        PropertiesBuffer = new ComputeBuffer(BrushCount, MeshProperties.Size());

        ComputeShader.SetBuffer(ComputePositionsKernel, "Vertices", VertexBuffer);
        ComputeShader.SetBuffer(ComputePositionsKernel, "Triangles", TriangleBuffer);
        ComputeShader.SetBuffer(ComputePositionsKernel, "_Properties", PropertiesBuffer);
        ComputeShader.SetVector("GlobalNormal", GlobalNormal);
        ComputeShader.SetMatrix("TRS", transform.localToWorldMatrix);
        Mat.SetBuffer("_Properties", PropertiesBuffer);

        ComputeShader.Dispatch(ComputePositionsKernel, Mathf.CeilToInt(BrushCount / 64.0f), 1, 1); 
        MeshProperties[] Data = new MeshProperties[BrushCount];
        PropertiesBuffer.GetData(Data);
    }

    private void OnDisable()
    {
        // Release gracefully.
        if (PropertiesBuffer != null)
        {
            PropertiesBuffer.Release();
        }
        PropertiesBuffer = null;
        
        if (VertexBuffer != null)
        {
            VertexBuffer.Release();
        }
        VertexBuffer = null;

        if (TriangleBuffer != null)
        {
            TriangleBuffer.Release();
        }
        TriangleBuffer = null;

        if (ArgsBuffer != null)
        {
            ArgsBuffer.Release();
        }
        ArgsBuffer = null;
    }
}
