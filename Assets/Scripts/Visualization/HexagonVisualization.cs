using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using Unity.Mathematics;
using System.Threading;
using static MapGenerator;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
/**
 * Visualization for a specific hexagon, contained in a @ChunkVisualization
 * Should always be representative for one @HexagonData
 * Will be reused if the actual hex location changes to save performance
 */ 
public class HexagonVisualization : MonoBehaviour, Selectable
{
    public void Init(ChunkData ChunkData, Location Location, Material Mat) {
        this.transform.position = Location.WorldLocation;
        this.Location = Location;
        this.gameObject.layer = LayerMask.NameToLayer("Hexagon");
        this.name = "Hex " + Location.HexLocation;
        this.Mat = Mat;
        Chunk = ChunkData;
        Data = Chunk.HexDatas[Location.HexLocation.x, Location.HexLocation.y];

        Generator = Game.GetService<MapGenerator>();
        UpdateMesh();
        Data._OnDiscovery = UpdateMesh;
        ChunkData.Visualization?.FinishVisualization();
    }

    public void UpdateMesh()
    {
        MeshFilter Filter = GetComponent<MeshFilter>();
        Destroy(Filter.mesh);

        GenerateMesh(Mat);
        Renderer = GetComponent<MeshRenderer>();
        SetSelected(false, false);
        SetHovered(false);

        if (Data.GetDiscoveryState() == HexagonData.DiscoveryState.Unknown)
            return;

        if (!Game.TryGetService(out MapGenerator MapGenerator))
            return;

        MapGenerator.UpdateMapBounds(Chunk.Location, Location);
    }

    void GenerateMesh(Material Mat) {
        if (!TileMeshGenerator.TryCreateMesh(Data, out Mesh Mesh))
            return;

        MeshRenderer Renderer = GetComponent<MeshRenderer>();
        Renderer.material = Mat;

        MeshCollider Collider = GetComponent<MeshCollider>();
        Collider.sharedMesh = Mesh;

        MeshFilter Filter = GetComponent<MeshFilter>();
        Filter.mesh = Mesh;
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

    public void ClickOn(Vector2 PixelPos) { }

    public void Interact() {
        if (!Game.TryGetService(out Selector Selector))
            return;

        Card Card = Selector.GetSelectedCard();
        if (Card && Card is BuildingCard) {
            InteractBuildBuilding(Card as BuildingCard);
            return;
        } 

        HexagonVisualization SelectedHex = Selector.GetSelectedHexagon();
        if (SelectedHex) {
            InteractMoveUnit(SelectedHex);
            return;
        }

        UIElement SelectedUIElement = Selector.GetSelectedUIElement();
        if (SelectedUIElement is PlaceableHexagon)
        {
            InteractSwapType((PlaceableHexagon) SelectedUIElement);
            return;
        }
    }

    private void InteractSwapType(PlaceableHexagon PlaceableHex)
    {
        if (!Game.TryGetService(out MapGenerator MapGenerator))
            return;

        Data.Type = PlaceableHex.Type;
        Data.HexHeight = PlaceableHex.Height;
        if (!MapGenerator.TrySetHexagonData(Location, Data.HexHeight, Data.Type))
            return;

        if (!MapGenerator.TryGetChunkData(Location, out ChunkData Chunk))
            return;

        Chunk.Visualization?.RefreshTokens();
        GenerateMesh(MapGenerator.HexMat);
        VisualizeSelection();

        if (!Game.TryGetService(out MiniMap Minimap))
            return;

        Minimap.FillBuffer();
    }

    private void InteractMoveUnit(HexagonVisualization SelectedHex) {
        if (!Game.TryGetServices(out Units UnitService, out Stockpile Stockpile))
            return;

        if (!UnitService.TryGetUnitAt(SelectedHex.Location, out TokenizedUnitData UnitOnTile))
            return;

        // can't move if there already is a unit!
        if (UnitService.TryGetUnitAt(this.Location, out TokenizedUnitData UnitAtTarget))
            return;

        if (!Stockpile.CanAfford(UnitOnTile.GetMovementRequirements()))
            return;

        List<Location> Path = Pathfinding.FindPathFromTo(SelectedHex.Location, this.Location);
        int PathCosts = Pathfinding.GetCostsForPath(Path);
        if (Path.Count == 0 || PathCosts > UnitOnTile.RemainingMovement)
            return;

        InteractMoveUnit(UnitOnTile, PathCosts);
    }

    private void InteractMoveUnit(TokenizedUnitData Unit, int Costs)
    {
        if (!Game.TryGetServices(out Selector Selector, out Stockpile Stockpile))
            return;

        // trigger movement range update
        Selector.DeselectHexagon();

        if (!Stockpile.Pay(Unit.GetMovementRequirements()))
            return;

        // update both chunks where the worker was and is going to
        if (!Generator.TryGetChunkData(Unit.Location, out ChunkData Chunk))
            return;

        Unit.MoveTo(this.Location, Costs);

        Chunk.Visualization.RefreshTokens();

        if (!Generator.TryGetChunkData(Unit.Location, out Chunk))
            return;

        Chunk.Visualization.RefreshTokens();

        if (!Generator.TryGetHexagon(Unit.Location, out HexagonVisualization NewHex))
            return;

        Selector.SelectHexagon(NewHex);
        _OnMovementTo?.Invoke(NewHex.Location);
    }

    private void InteractBuildBuilding(BuildingCard Card)
    {
        if (!Game.TryGetServices(out Selector Selector, out Stockpile Stockpile))
            return;

        if (Generator.IsBuildingAt(Location)) {
            MessageSystem.CreateMessage(Message.Type.Error, "Cannot create building here - one already exists");
            return;
        }

        BuildingData Building = Card.GetBuildingData();
        if (!Building.CanBeBuildOn(this, false)) {
            MessageSystem.CreateMessage(Message.Type.Error, "Cannot create building here - invalid placement");
            return;
        }

        if (!Stockpile.Pay(Building.GetCosts())) {
            MessageSystem.CreateMessage(Message.Type.Error, "Cannot create building here - not enough resources");
            return;
        }

        BuildBuildingFromCard(Building);
        Selector.ForceDeselect();
        Selector.SelectHexagon(this);

        Card.Use();
    }

    private void BuildBuildingFromCard(BuildingData Building) {
        Building.Location = Location.Copy();
        Generator.AddBuilding(Building);
    }

    public bool IsEqual(Selectable other) {
        if (other is not HexagonVisualization)
            return false;

        HexagonVisualization OtherHex = other as HexagonVisualization;
        return Location.GlobalTileLocation.x == OtherHex.Location.GlobalTileLocation.x && Location.GlobalTileLocation.y == OtherHex.Location.GlobalTileLocation.y;
    }

    public ChunkData GetChunk() { return Chunk; }

    private void UpdateBuildingPreview()
    {
        if (!Game.TryGetServices(out Selector Selector, out BuildingPreview BuildingPreview))
            return;

        Card SelectedCard = Selector.GetSelectedCard();
        if (!SelectedCard || !isHovered || SelectedCard is not BuildingCard) {
            BuildingPreview.Hide();
            ShowAdjacencyBonus(null);
            return;
        }

        BuildingPreview.Show(SelectedCard as BuildingCard, this);
        ShowAdjacencyBonus(SelectedCard);
    }

    private void ShowAdjacencyBonus(Card SelectedCard) {
        BuildingCard SelectedBuildingCard = SelectedCard as BuildingCard;
        BuildingData Building = SelectedBuildingCard ? SelectedBuildingCard.GetBuildingData() : null;
        bool bIsVisible = Building != null ? Building.CanBeBuildOn(this) : false;

        // check for each neighbour if it should be highlighted
        int Range = Building != null ? Building.Effect.Range : 2;
        List<HexagonVisualization> Neighbours = Generator.GetNeighbours(this, true, Range);
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
                if (Generator.IsBuildingAt(Neighbour.Location) && Building.IsNeighbourBuildingBlocking()) {
                    bIsAdjacent = false;
                }
            }
            Neighbour.isAdjacent = bIsAdjacent;
            Neighbour.VisualizeSelection();
        }
    }

    public void ShowReachableLocations(bool bShow)
    {
        if (!Game.TryGetServices(out Units UnitService, out Stockpile Stockpile))
            return;

        // always query, just reset if null
        UnitService.TryGetUnitAt(Location, out TokenizedUnitData Unit);

        bool bCanPay = Unit != null && Stockpile.CanAfford(Unit.GetMovementRequirements());

        bool bIsVisible = Unit != null && bShow && bCanPay;
        int Range = Unit != null ? Unit.RemainingMovement : 0;

        // check for each reachable tile if it should be highlighted
        HashSet<Location> ReachableLocations = Pathfinding.FindReachableLocationsFrom(Location, Range);

        foreach (Location ReachableLocation in ReachableLocations) {
            if (!Generator.TryGetHexagon(ReachableLocation, out HexagonVisualization ReachableHex))
                continue;

            ReachableHex.isReachable = bIsVisible;
            ReachableHex.VisualizeSelection();
        }
    }

    public void UpdateDiscoveryState(int VisitingRange, int ScoutingRange)
    {
        if (!Game.TryGetService(out MapGenerator MapGenerator))
            return;

        HashSet<Location> DirectNeighbours = GetNeighbourTileLocationsInRange(this.Location, true, VisitingRange);
        HashSet<Location> OtherNeighbours = GetNeighbourTileLocationsInRange(this.Location, false, ScoutingRange);
        OtherNeighbours.ExceptWith(DirectNeighbours);

        foreach (Location Location in DirectNeighbours)
        {
            if (!MapGenerator.TryGetHexagonData(Location, out HexagonData NeighbourHex))
                continue;

            NeighbourHex.UpdateDiscoveryState(HexagonData.DiscoveryState.Visited);
        }

        foreach (Location Location in OtherNeighbours)
        {
            if (!MapGenerator.TryGetHexagonData(Location, out HexagonData NeighbourHex))
                continue;

            NeighbourHex.UpdateDiscoveryState(HexagonData.DiscoveryState.Scouted);
        }
    }

    public void VisualizeSelection() {
        MaterialPropertyBlock Block = new MaterialPropertyBlock();
        Block.SetFloat("_Selected", isSelected ? 1 : 0);
        Block.SetFloat("_Hovered", isHovered ? 1 : 0);
        Block.SetFloat("_Adjacent", isAdjacent || isReachable ? 1 : 0);
        if (Data != null) {
            Block.SetFloat("_Malaised", Data.bIsMalaised ? 1 : 0);
            Block.SetFloat("_Type", HexagonConfig.MaskToInt((int)Data.Type, 16) + 1);
            Block.SetFloat("_Value", Data.DebugValue);
        }
        if (!Renderer)
            return;

        Renderer.SetPropertyBlock(Block);
    }

    public delegate void OnMovementTo(Location Location);
    public static event OnMovementTo _OnMovementTo;

    public Location Location;
    public ChunkData Chunk;
    public HexagonData Data;

    protected bool isHovered, isSelected, isAdjacent, isReachable, isPathway;

    protected Vector3[] Vertices;
    protected int[] Triangles;

    private MeshRenderer Renderer;
    private MapGenerator Generator;
    private Material Mat;
}
