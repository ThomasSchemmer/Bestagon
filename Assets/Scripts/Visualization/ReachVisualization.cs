using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//todo: highly inefficient
/** 
 * Visualizes the reach around already placed buildings
 * To minimize "spreading" aka placing buildings super far away without restrictions
 * the player should only be able to place them close to the other buildings 
 * Lore reason: Too far apart makes communication and transportation of goods impossible
 */
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class ReachVisualization : GameService
{
    public Material Mat;

    private HashSet<Location> BuildingLocations = new();
    private Dictionary<Location, byte> Corners = new();
    private HashSet<Location> Set = new();

    private MeshFilter MeshFilter;
    private MeshRenderer Renderer;

    private List<Vector3> Vertices = new();
    private List<int> Triangles = new();
    private float Width = 0.3f;

    public bool CheckFor(Location Location)
    {
        // first building is always allowed
        if (Set.Count == 0)
        {
            Display(true, false);
            return true;
        }

        bool bIsInSet = Set.Contains(Location);
        Display(bIsInSet, true);
        return bIsInSet;
    }

    public void Hide()
    {
        Display(false, false);
    }

    private void Display(bool bIsAllowed, bool bIsVisible)
    {
        if (!IsInit)
            return;

        Mat.color = bIsAllowed ? PositiveColor : NegativeColor;
        // set the renderer, not the GO, as it would kill the coroutines!
        Renderer.enabled = bIsVisible;
    }

    private void GenerateMesh()
    {
        Vertices.Clear();
        Triangles.Clear();

        foreach (var Tuple in Corners)
        {
            byte Mask = Tuple.Value;
            for (int i = 0; i < 6; i++)
            {
                bool bIsBorder = ((Mask >> i) & 0x1) == 1;
                if (!bIsBorder)
                    continue;

                int A = i;
                int B = (i + 1 + 6) % 6;

                Vector3 WorldPosA = Tuple.Key.WorldLocation + HexagonConfig.GetVertex(A) + Offset;
                Vector3 WorldPosB = Tuple.Key.WorldLocation + HexagonConfig.GetVertex(B) + Offset;
                GenerateMesh(WorldPosA, WorldPosB);
            }
        }

        if (MeshFilter.mesh != null)
        {
            DestroyImmediate(MeshFilter.mesh);
        }
        Mesh Mesh = new Mesh();
        Mesh.vertices = Vertices.ToArray();
        Mesh.triangles = Triangles.ToArray();
        Mesh.RecalculateBounds();
        Mesh.RecalculateNormals();
        MeshFilter.mesh = Mesh;
    }

    private void GenerateMesh(Vector3 Start, Vector3 End)
    {
        Vector3 Forward = End - Start;
        Forward = new Vector3(Forward.x, 0, Forward.z).normalized;
        // only works cause y is 0!
        Vector3 Sideways = new Vector3(Forward.z, 0, -Forward.x) * Width;

        Vector3 A = Start + Sideways;
        Vector3 B = Start - Sideways;
        Vector3 C = End + Sideways;
        Vector3 D = End - Sideways;

        int Count = Triangles.Count;
        Vertices.AddRange(new[] { A, B, C });
        Vertices.AddRange(new[] { C, B, D });
        Triangles.AddRange(new[] { Count + 0, Count + 1, Count + 2 });
        Triangles.AddRange(new[] { Count + 3, Count + 4, Count + 5 });
    }

    private void AddReach(BuildingEntity AddedBuilding)
    {
        BuildingLocations.Add(AddedBuilding.GetLocation());
        AddReach(AddedBuilding.GetLocation());
    }

    private void AddReach(Location Location, bool bShouldUpdateBorders = true)
    {
        int Reach = (int)AttributeSet.Get()[AttributeType.BuildingReach].CurrentValue;
        Set.UnionWith(Pathfinding.FindReachableLocationsFrom(Location, Reach, false));
        if (!bShouldUpdateBorders)
            return;

        UpdateBorders();
        GenerateMesh();
    }

    private void UpdateBorders()
    {
        Corners = new();

        // check each tile if its a border tile (aka min one neighbour isnt in the set)
        // slow through double loop but at least lookup is quick, ty set
        foreach (var Location in Set)
        {
            byte NeighbourMask = 0;
            int Count = 0;
            List<Location> Neighbours = MapGenerator.GetNeighbourTileLocations(Location);
            for (int i = 0; i < Neighbours.Count; i++)
            {
                bool bIsInReach = Set.Contains(Neighbours[i]);
                if (bIsInReach)
                    continue;

                NeighbourMask |= (byte)(1 << i);
                Count++;
            }

            if (NeighbourMask == 0)
                continue;

            if (Corners.ContainsKey(Location))
            {
                Corners.Remove(Location);
            }
            Corners.Add(Location, NeighbourMask);
        }
    }

    private void RemoveReach(BuildingEntity RemovedBuilding)
    {
        BuildingLocations.Remove(RemovedBuilding.GetLocation());
        Set = new();
        // dont auto update everytime
        foreach (Location ReachLocation in BuildingLocations)
        {
            AddReach(ReachLocation, false);
        }
        var Enumerator = BuildingLocations.GetEnumerator();
        if (!Enumerator.MoveNext())
            return;

        AddReach(Enumerator.Current);
    }

    protected override void StartServiceInternal()
    {
        MeshFilter = GetComponent<MeshFilter>();
        Renderer = GetComponent<MeshRenderer>();
        Renderer.material = Mat;


        BuildingService._OnBuildingBuilt.Add(AddReach);
        BuildingService._OnBuildingDestroyed.Add(RemoveReach);
        
        _OnInit?.Invoke(this);
    }

    private void OnDestroy()
    {
        BuildingService._OnBuildingBuilt.Remove(AddReach);
        BuildingService._OnBuildingDestroyed.Remove(RemoveReach);
    }

    protected override void StopServiceInternal(){}

    private static Vector3 Offset = new Vector3(0, HexagonConfig.TileSize.y + 1, 0);
    private static Color PositiveColor = Color.yellow;
    private static Color NegativeColor = Color.red;

}
