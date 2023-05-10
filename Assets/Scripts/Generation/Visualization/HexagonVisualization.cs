using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(HexagonDebug))]
[RequireComponent(typeof(MeshCollider))]
public class HexagonVisualization : MonoBehaviour, Selectable
{
    public void Init(ChunkData ChunkData, Location Location, Material Mat, List<Mesh> Meshes) {
        this.transform.position = Location.WorldLocation;
        this.Location = Location;
        this.gameObject.layer = LayerMask.NameToLayer("Hexagon");
        this.name = "Hex " + Location.HexLocation;
        Chunk = ChunkData;
        Data = Chunk.HexDatas[Location.HexLocation.x, Location.HexLocation.y];
        GenerateMesh(Mat, Meshes[(int)Data.Type - 1]);
        Renderer = GetComponent<MeshRenderer>();
        SetSelected(false, false);
        SetHovered(false);
    }

    void GenerateMesh(Material Mat, Mesh InMesh) {
        Mesh mesh = InMesh;

        // force a copy
        MeshFilter Filter = GetComponent<MeshFilter>();
        Filter.mesh.Clear();
        Filter.mesh.vertices = mesh.vertices;
        Filter.mesh.triangles = mesh.triangles;
        Filter.mesh.uv = mesh.uv;
        Filter.mesh.RecalculateBounds();
        Filter.mesh.RecalculateNormals();

        MeshRenderer Renderer = GetComponent<MeshRenderer>();
        Renderer.material = Mat;

        MeshCollider Collider = GetComponent<MeshCollider>();
        Collider.sharedMesh = Filter.mesh;
    }

    public void SetSelected(bool Selected, bool bShowReachableLocations) {
        isSelected = Selected;
        VisualizeSelection();
        UpdateBuildingPreview();
        if (bShowReachableLocations) {
            ShowReachableLocations(Selected);
        }
    }

    public void SetSelected(bool Selected) {
        SetSelected(Selected, true);
    }

    public void SetHovered(bool Hovered) {
        isHovered = Hovered;
        VisualizeSelection();
        UpdateBuildingPreview();
    }

    public void Interact() {
        Card Card = Selector.GetSelectedCard();
        if (Card) {
            InteractBuildBuilding(Card);
            return;
        } 

        HexagonVisualization SelectedHex = Selector.GetSelectedHexagon();
        if (SelectedHex) {
            InteractMoveWorker(SelectedHex);
            return;
        }
    }

    private void InteractMoveWorker(HexagonVisualization SelectedHex) {
        if (!Workers.TryGetWorkersAt(SelectedHex.Location, out List<WorkerData> WorkersOnTile))
            return;

        WorkerData Worker = WorkersOnTile[0];
        List<Location> Path = Pathfinding.FindPathFromTo(SelectedHex.Location, this.Location);
        int PathCosts = Pathfinding.GetCostsForPath(Path);
        if (Path.Count == 0 || PathCosts > Worker.RemainingMovement)
            return;

        InteractMoveWorker(Worker, PathCosts);
    }

    private void InteractMoveWorker(WorkerData Worker, int Costs) {
        // trigger movement range update
        Selector.DeselectHexagon();

        // update both chunks where the worker was and is going to
        if (!MapGenerator.TryGetChunkData(Worker.Location, out ChunkData Chunk))
            return;

        Chunk.Visualization.Refresh();

        Worker.MoveTo(this.Location, Costs);

        if (!MapGenerator.TryGetChunkData(Worker.Location, out Chunk))
            return;

        Chunk.Visualization.Refresh();

        if (!MapGenerator.TryGetHexagon(Worker.Location, out HexagonVisualization NewHex))
            return;

        Selector.SelectHexagon(NewHex);
    }

    private void InteractBuildBuilding(Card Card) {
        if (MapGenerator.IsBuildingAt(Location)) {
            MessageSystem.CreateMessage(Message.Type.Error, "Cannot create building here - one already exists");
            return;
        }

        BuildingData Building = Card.GetBuildingData();
        if (!Building.CanBeBuildOn(this)) {
            MessageSystem.CreateMessage(Message.Type.Error, "Cannot create building here - invalid placement");
            return;
        }

        if (!Stockpile.Pay(Building.GetCosts())) {
            MessageSystem.CreateMessage(Message.Type.Error, "Cannot create building here - not enough resources");
            return;
        }

        BuildBuildingFromCard(Building);
        Selector.ForceDeselect();
        CardHand.DiscardCard(Card);
        Selector.SelectHexagon(this);
    }

    private void BuildBuildingFromCard(BuildingData Building) {
        Building.Location = Location.Copy();
        MapGenerator.AddBuilding(Building);
    }

    public bool IsEqual(Selectable other) {
        if (other is not HexagonVisualization)
            return false;

        HexagonVisualization OtherHex = other as HexagonVisualization;
        return Location.HexLocation.x == OtherHex.Location.HexLocation.x && Location.HexLocation.y == OtherHex.Location.HexLocation.y;
    }

    public ChunkData GetChunk() { return Chunk; }

    private void UpdateBuildingPreview() {
        Card SelectedCard = Selector.GetSelectedCard();
        if (!SelectedCard || !isHovered) {
            BuildingPreview.Hide();
            ShowAdjacencyBonus(null);
            return;
        }

        BuildingPreview.Show(SelectedCard, this);
        ShowAdjacencyBonus(SelectedCard);
    }

    private void ShowAdjacencyBonus(Card SelectedCard) {
        BuildingData Building = SelectedCard ? SelectedCard.GetBuildingData() : null;
        bool bIsVisible = Building != null ? Building.CanBeBuildOn(this) : false;

        // check for each neighbour if it should be highlighted
        List<HexagonVisualization> Neighbours = MapGenerator.GetNeighbours(this);
        Dictionary<HexagonConfig.HexagonType, Production> Boni = new();
        if (Building != null) {
            Building.TryGetAdjacencyBonus(out Boni);
        }

        foreach (HexagonVisualization Neighbour in Neighbours) {
            bool bIsAdjacent = false;
            if (bIsVisible) {
                if (Boni.TryGetValue(Neighbour.Data.Type, out _)) {
                    bIsAdjacent = true;
                }
                if (MapGenerator.IsBuildingAt(Neighbour.Location) && Building.IsNeighbourBuildingBlocking()) {
                    bIsAdjacent = false;
                }
            }
            Neighbour.isAdjacent = bIsAdjacent;
            Neighbour.VisualizeSelection();
        }
    }

    private void ShowReachableLocations(bool bShow) {
        Workers.TryGetWorkersAt(Location, out List<WorkerData> WorkersOnTile);
        WorkerData Worker = WorkersOnTile.Count > 0 ? WorkersOnTile[0] : null;
        bool bIsVisible = Worker != null && bShow;
        int Range = Worker != null ? Worker.RemainingMovement : 0;

        // check for each reachable tile if it should be highlighted
        HashSet<Location> ReachableLocations = Pathfinding.FindReachableLocationsFrom(Location, Range);

        foreach (Location ReachableLocation in ReachableLocations) {
            if (!MapGenerator.TryGetHexagon(ReachableLocation, out HexagonVisualization ReachableHex))
                continue;

            ReachableHex.isReachable = bIsVisible;
            ReachableHex.VisualizeSelection();
        }
    }

    public void VisualizeSelection() {
        MaterialPropertyBlock Block = new MaterialPropertyBlock();
        Block.SetFloat("_Selected", isSelected ? 1 : 0);
        Block.SetFloat("_Hovered", isHovered ? 1 : 0);
        Block.SetFloat("_Adjacent", isAdjacent || isReachable ? 1 : 0);
        if (Data != null) {
            Block.SetFloat("_Malaised", Data.bIsMalaised ? 1 : 0);
            Block.SetFloat("_Type", (float)Data.Type);
        }
        if (!Renderer)
            return;

        Renderer.SetPropertyBlock(Block);
    }

    public Location Location;
    public ChunkData Chunk;
    public HexagonData Data;

    protected bool isHovered, isSelected, isAdjacent, isReachable, isPathway;

    protected Vector3[] Vertices;
    protected int[] Triangles;

    private MeshRenderer Renderer;
}
