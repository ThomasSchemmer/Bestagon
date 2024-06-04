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
public class HexagonVisualization : MonoBehaviour, ISelectable
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
        SetSelected(false, false, true);
        SetHovered(false);

        if (Data.GetDiscoveryState() == HexagonData.DiscoveryState.Unknown)
            return;

        if (!Game.TryGetService(out MapGenerator MapGenerator))
            return;

        MapGenerator.UpdateMapBounds(Chunk.Location, Location);
    }

    public void GenerateMesh(Material Mat) {
        if (!TileMeshGenerator.TryCreateMesh(Data, out Mesh Mesh))
            return;

        MeshRenderer Renderer = GetComponent<MeshRenderer>();
        Renderer.material = Mat;

        MeshCollider Collider = GetComponent<MeshCollider>();
        Collider.sharedMesh = Mesh;

        MeshFilter Filter = GetComponent<MeshFilter>();
        Filter.mesh = Mesh;
    }

    public void SetSelected(bool Selected, bool bShowReachableLocations, bool bIsInitializing = false) {
        isSelected = Selected;
        VisualizeSelection();
        if (bShowReachableLocations) {
            ShowReachableLocations(Selected);
        }

        // UpdatePreview doesn't need to be spammed
        if (bIsInitializing)
            return;

        if (!Game.TryGetService(out PreviewSystem Preview))
            return;

        Preview.UpdatePreview();
    }

    public void SetSelected(bool Selected) {
        SetSelected(Selected, true);
    }

    public void SetHovered(bool Hovered) {
        isHovered = Hovered;
        VisualizeSelection();

        if (!Game.TryGetService(out PreviewSystem Preview))
            return;

        Preview.UpdatePreview();
    }

    public bool IsHovered()
    {
        return isHovered;
    }

    public void ClickOn(Vector2 PixelPos) { }

    public void Interact() {
        if (!Game.TryGetServices(out Selectors Selector, out PreviewSystem Preview))
            return;

        Card Card = Selector.GetSelectedCard();
        if (Card && Card.IsCardInteractableWith(this)) {
            Card.InteractWith(this);
            Selector.DeselectCard();
            Preview.UpdatePreview();
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

    public void InteractMoveUnit(HexagonVisualization SelectedHex) {
        if (!Game.TryGetServices(out Units UnitService, out Stockpile Stockpile))
            return;

        if (!UnitService.TryGetUnitAt(SelectedHex.Location, out TokenizedUnitData UnitOnTile))
            return;

        // can't move if there already is a unit!
        if (UnitService.TryGetUnitAt(this.Location, out TokenizedUnitData UnitAtTarget))
            return;

        if (!Stockpile.CanAfford(UnitOnTile.GetMovementRequirements()) || UnitOnTile.IsStarving())
            return;

        List<Location> Path = Pathfinding.FindPathFromTo(SelectedHex.Location, this.Location);
        int PathCosts = Pathfinding.GetCostsForPath(Path);
        if (Path.Count == 0 || PathCosts > UnitOnTile.RemainingMovement)
            return;

        InteractMoveUnit(UnitOnTile, PathCosts);
    }

    private void InteractMoveUnit(TokenizedUnitData Unit, int Costs)
    {
        if (!Game.TryGetServices(out Selectors Selector, out Stockpile Stockpile))
            return;

        // trigger movement range update
        Selector.DeselectHexagon();

        if (!Stockpile.Pay(Unit.GetMovementRequirements()))
            return;

        Unit.MoveTo(this.Location, Costs);

        if (!Generator.TryGetHexagon(Unit.Location, out HexagonVisualization NewHex))
            return;

        Selector.SelectHexagon(NewHex);
    }

    public bool IsEqual(ISelectable other) {
        if (other is not HexagonVisualization)
            return false;

        HexagonVisualization OtherHex = other as HexagonVisualization;
        return Location.GlobalTileLocation.x == OtherHex.Location.GlobalTileLocation.x && Location.GlobalTileLocation.y == OtherHex.Location.GlobalTileLocation.y;
    }

    public bool IsMalaised()
    {
        return Data.IsMalaised();
    }

    public ChunkData GetChunk() { return Chunk; }

    public void ShowReachableLocations(bool bShow)
    {
        if (!Game.TryGetServices(out Units UnitService, out Stockpile Stockpile))
            return;

        // always query, just reset if null
        UnitService.TryGetUnitAt(Location, out TokenizedUnitData Unit);

        bool bCanPay = Unit != null && Stockpile.CanAfford(Unit.GetMovementRequirements()) && !Unit.IsStarving();

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
        if (Data != null)
        {
            Block.SetFloat("_Malaised", Data.IsMalaised() ? 1 : 0);
            Block.SetFloat("_PreMalaised", Data.IsPreMalaised() ? 1 : 0);
            Block.SetFloat("_Type", HexagonConfig.MaskToInt((int)Data.Type, 16) + 1);
            Block.SetFloat("_Value", Data.DebugValue);
        }
        if (!Renderer)
            return;

        Renderer.SetPropertyBlock(Block);
    }

    public bool CanBeLongHovered()
    {
        return true;
    }

    public void SetAdjacent(bool bIsAdjacent)
    {
        isAdjacent = bIsAdjacent;
    }

    public string GetHoverTooltip()
    {
        return "This tile represents a part of the world, providing space for units or buildings. Can be used to produce resources";
    }

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
