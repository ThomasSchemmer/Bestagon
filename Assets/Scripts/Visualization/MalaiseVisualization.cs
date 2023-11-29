using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class MalaiseVisualization : MonoBehaviour
{
    public void Initialize(MalaiseData Data, Material Mat)
    {
        this.name = "MalaiseVisualization";
        this.Data = Data;
        Filter = GetComponent<MeshFilter>();
        Mesh = Filter.mesh;
        GetComponent<MeshRenderer>().material = Mat;

        GenerateMesh();
    }

    public void GenerateMesh() {
        List<Vector3> Vertices = new List<Vector3>();
        List<int> Triangles = new List<int>();
        List<Vector2> UVs = new List<Vector2>();
        Mesh.Clear();
        if (Data == null)
            return;

        List<HexagonData> MalaisedHexes = Data.GetMalaisedHexes();
        if (MalaisedHexes.Count == 0)
            return;

        foreach (HexagonData HexagonData in MalaisedHexes) {
            int Count = Vertices.Count;
            Vertices.AddRange(GetVertices(HexagonData));
            Triangles.AddRange(GetTriangles(Count));
            UVs.AddRange(GetUVs(HexagonData));
        }

        Mesh.vertices = Vertices.ToArray();
        Mesh.triangles = Triangles.ToArray();
        Mesh.uv = UVs.ToArray();
        Mesh.RecalculateBounds();
        Mesh.RecalculateNormals();
        Mesh.RecalculateTangents();
    }

    List<Vector2> GetUVs(HexagonData HexagonData) {
        // check for each corner points (index 0..6) whether the direct neighbours are malaised
        // for each of them increase the uv.x by 0.5, so that we can use this value in our shader to
        // simulate "borders"
        // the mapping of vertex -> neighbour is implicitly done with the directions,
        // where 0 -> top left neighbour and then clockwise
        int Count = 7;
        List<Vector2> UVs = new List<Vector2>();
        float[] UVSum = new float[Count];

        HexagonData[] NeighbourData = MapGenerator.GetNeighboursDataArray(HexagonData.Location);
        bool bFullyMalaised = true;
        for (int i = 0; i < Count; i++) {
            // check left and right neighbour
            int A = i % NeighbourData.Length;
            int B = (i - 1 + NeighbourData.Length) % NeighbourData.Length;
            int MalaisedNeighbourCount = 0;
            MalaisedNeighbourCount += NeighbourData[A] != null ? (NeighbourData[A].bIsMalaised ? 1 : 0) : 0;
            MalaisedNeighbourCount += NeighbourData[B] != null ? (NeighbourData[B].bIsMalaised ? 1 : 0) : 0;
            UVSum[i] = MalaisedNeighbourCount == 2 ? 1 : MalaisedNeighbourCount * 0.25f;

            if (MalaisedNeighbourCount != 2) {
                bFullyMalaised = false;
            }
        }

        // add center uv which should always be visible
        UVs.Add(new Vector2(bFullyMalaised ? 1f : 0.25f, 0));

        for (int i = 0; i < Count; i++) {
            UVs.Add(new Vector2(UVSum[i], 0));
        }

        return UVs;
    }

    List<Vector3> GetVertices(HexagonData HexagonData) { 
        List<Vector3> Vertices = new List<Vector3>();
        Vector3 WorldLocation = HexagonData.Location.WorldLocation + Offset;
        Vertices.Add(WorldLocation);
        for (int i = 0; i < 7; i++) {
            Vector3 Position = HexagonConfig.GetVertex(i) + WorldLocation;
            Vertices.Add(Position);
        }
        return Vertices;
    }

    List<int> GetTriangles(int Count) {
        //build triangles from the center outwards, clockwise
        List<int> Triangles = new() {
            Count, Count + 1, Count + 2,
            Count, Count + 2, Count + 3,
            Count, Count + 3, Count + 4,
            Count, Count + 4, Count + 5,
            Count, Count + 5, Count + 6,
            Count, Count + 6, Count + 7
        };

        return Triangles;

    }

    private MeshFilter Filter;
    private Mesh Mesh;

    private MalaiseData Data;

    public static Vector3 Offset = new Vector3(-7, 20, -7);
}
