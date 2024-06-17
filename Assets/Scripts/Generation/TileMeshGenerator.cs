using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

/**
 * Helper class to create custom meshes for the different tiles
 */
public class TileMeshGenerator : MonoBehaviour
{
    private static List<Vector3> Vertices;
    private static List<int> Triangles;
    private static List<Vector2> UVs;

    public static bool TryCreateMesh(HexagonData Data, out Mesh Mesh)
    {
        Mesh = null;
        Vertices = new();
        Triangles = new();
        UVs = new();
        Profiler.BeginSample("CreateMesh_Base");
        if (!TryCreateBaseData(Data))
            return false;
        Profiler.EndSample();
        Profiler.BeginSample("CreateMesh_AddTile");
        if (!TryAddTile(Data))
            return false;

        Profiler.EndSample();
        Profiler.BeginSample("CreateMesh_Fill");
        Mesh = CreateAndFillMesh();

        Profiler.EndSample();
        return true;
    }

    public static Vector3[] CreatePolygonRing(Vector3 Center)
    {
        Vector3[] Polygon = new Vector3[7];
        for (int i = 0; i < 7; i++)
        {
            Polygon[i] = Center + HexagonConfig.GetVertex(i % 6);
        }
        return Polygon;
    }

    private static Mesh CreateAndFillMesh() {
        Mesh Mesh = new Mesh();
        Mesh.vertices = Vertices.ToArray();
        Mesh.triangles = Triangles.ToArray();
        Mesh.uv = UVs.ToArray();
        Mesh.RecalculateBounds();
        Mesh.RecalculateNormals();

        return Mesh;
    }

    private static bool TryCreateBaseData(HexagonData Data) {
        if (!TryCreateBaseVertices(Data))
            return false;
        if (!TryCreateBaseTriangles(Data))
            return false;
        if (!TryCreateBaseUVs(Data))
            return false;
        return true;
    }

    private static bool TryCreateBaseUVs(HexagonData Data) { 
        // every uv is only important for x, as y will be set by shader
        // uv.x is in range of 0..1, but will be interpreted as 1..16 to match color info
        // uv is on per vertex base, so pretty much duplicate vertex generation here

        // upper ring, for outside
        for (int i = 0; i < 6; i++) {
            UVs.Add(new Vector2(0.1f / 16.0f, 0));
        }

        // lower ring
        for (int i = 0; i < 6; i++) {
            UVs.Add(new Vector2(0.1f / 16.0f, 0));
        }

        //lower center
        UVs.Add(new Vector2(0.1f / 16.0f, 0));

        // upper ring, for top side
        for (int i = 0; i < 6; i++) {
            UVs.Add(new Vector2(0.1f / 16.0f, 0));
        }

        // upper inner ring, for top side
        for (int i = 0; i < 6; i++) {
            UVs.Add(new Vector2(0.1f / 16.0f, 0));
        }

        // upper inner ring, for inset
        for (int i = 0; i < 6; i++) {
            UVs.Add(new Vector2(0.1f / 16.0f, 0));
        }

        // upper inner inset
        for (int i = 0; i < 6; i++) {
            UVs.Add(new Vector2(0.1f / 16.0f, 0));
        }
        return true;
    }

    private static bool TryCreateBaseTriangles(HexagonData Data) {
        // outer ring
        for (int i = 0; i < 6; i++) {
            int j = (i + 1) % 6;
            Triangles.AddRange(new int[] { j, i, i + 6 });
            Triangles.AddRange(new int[] { j + 6, j, i + 6 });
        }

        // bottom
        for (int i = 0; i < 6; i++) {
            int j = (i + 1) % 6;
            Triangles.AddRange(new int[] { j + 6, i + 6, 12});
        }

        // upper ring
        for (int i = 0; i < 6; i++) {
            int j = (i + 1) % 6;
            Triangles.AddRange(new int[] { i + 13, j + 13, i + 19 });
            Triangles.AddRange(new int[] { j + 13, j + 19, i + 19 });
        }

        // inset ring
        for (int i = 0; i < 6; i++) {
            int j = (i + 1) % 6;
            Triangles.AddRange(new int[] { i + 25, j + 25, i + 31 });
            Triangles.AddRange(new int[] { j + 25, j + 31, i + 31 });
        }
        return true;
    }

    private static bool TryCreateBaseVertices(HexagonData Data) {
        Vector3 HeightOffset = new Vector3(0, Data.WorldHeight, 0);

        // upper ring
        for (int i = 0; i < 6; i++) {
            Vertices.Add(HexagonConfig.GetVertex(i) + HeightOffset);
        }

        // lower ring
        for (int i = 0; i < 6; i++) {
            Vertices.Add(HexagonConfig.GetVertex(i));
        }

        //lower center
        Vertices.Add(new Vector3(0, 0, 0));

        // since unity has uv per vertex, we need to duplicate the border between top and sides/insets
        for (int i = 0; i < 6; i++) {
            Vertices.Add(HexagonConfig.GetVertex(i) + HeightOffset);
        }

        // upper inner ring
        for (int i = 0; i < 6; i++) {
            Vector3 Vertex = HexagonConfig.GetVertex(i) + HeightOffset;
            Vertex.x *= HexagonConfig.TileBorderWidthMultiplier;
            Vertex.z *= HexagonConfig.TileBorderWidthMultiplier;
            Vertices.Add(Vertex);
        }

        // upper inner ring, for inset
        for (int i = 0; i < 6; i++) {
            Vector3 Vertex = HexagonConfig.GetVertex(i) + HeightOffset;
            Vertex.x *= HexagonConfig.TileBorderWidthMultiplier;
            Vertex.z *= HexagonConfig.TileBorderWidthMultiplier;
            Vertices.Add(Vertex);
        }

        // inner inset
        for (int i = 0; i < 6; i++) {
            Vector3 Vertex = HexagonConfig.GetVertex(i) + HeightOffset;
            Vertex.x *= HexagonConfig.TileBorderWidthMultiplier;
            Vertex.z *= HexagonConfig.TileBorderWidthMultiplier;
            Vertex.y *= HexagonConfig.TileBorderHeightMultiplier;
            Vertices.Add(Vertex);
        }
        return true;
    }

    private static bool TryAddTile(HexagonData Data) {
        if (!Game.TryGetService(out MeshFactory BuildingFactory))
            return false;

        Mesh TileMesh = null;
        Mesh DecorationMesh = null;
        switch (Data.GetDiscoveryState()) {
            case HexagonData.DiscoveryState.Unknown: break;
            case HexagonData.DiscoveryState.Scouted: TileMesh = BuildingFactory.UnknownMesh; break;
            case HexagonData.DiscoveryState.Visited: 
                TileMesh = BuildingFactory.GetMeshFromType(Data.Type);
                DecorationMesh = BuildingFactory.GetMeshFromType(Data.Decoration);
                break;
        }
        if (!TileMesh)
            return false;

        bool bIsVisited = Data.GetDiscoveryState() == HexagonData.DiscoveryState.Visited;
        if (bIsVisited && Data.Decoration != HexagonConfig.HexagonDecoration.None && DecorationMesh == null)
            return false;

        AddMesh(Data, TileMesh);
        if (DecorationMesh != null)
        {
            AddMesh(Data, DecorationMesh);
        }
        

        return true;
    }

    private static void AddMesh(HexagonData Data, Mesh Mesh)
    {
        int BaseVertexCount = Vertices.Count;
        Vector3 HeightOffset = new Vector3(0, Data.WorldHeight, 0) * HexagonConfig.TileBorderHeightMultiplier;

        foreach (Vector3 Vertex in Mesh.vertices)
        {
            Vertices.Add(Vertex + HeightOffset);
        }
        foreach (int Triangle in Mesh.triangles)
        {
            Triangles.Add(Triangle + BaseVertexCount);
        }
        foreach (Vector2 UV in Mesh.uv)
        {
            UVs.Add(UV);
        }
    }

}
