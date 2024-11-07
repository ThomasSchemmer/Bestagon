using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static MapGenerator;
using UnityEngine.Profiling;
using System;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]

/**
 * Visualization for a specific hexagon, contained in a @ChunkVisualization
 * Should always be representative for one @HexagonData
 * Will be reused if the actual hex location changes to save performance
 */ 
public class HexagonVisualization : MonoBehaviour, ISelectable
{
    [Flags]
    /** Describes the current interaction state of the visualization
    * Anything further down overrides further up (?)
    */
    public enum State : uint
    {
        None = 0,
        Hovered = 1,
        Selected = 1 << 1,
        
    }

    public void Init(ChunkVisualization ChunkVis, ChunkData ChunkData, Location Location, Material Mat)
    {
        Profiler.BeginSample("Init");
        this.transform.position = Location.WorldLocation;
        this.Location = Location;
        this.gameObject.layer = LayerMask.NameToLayer("Hexagon");
        this.name = "Hex " + Location.HexLocation;
        this.Mat = Mat;
        Chunk = ChunkData;

        if (!Chunk.TryGetHexAt(Location.HexLocation, out HexagonData Hex))
            return;

        Data = Hex;

        Generator = Game.GetService<MapGenerator>();
        UpdateMesh();
        Data._OnDiscovery = UpdateMesh;
        ChunkVis?.FinishVisualization();
        Profiler.EndSample();
    }

    public void UpdateMesh()
    {
        Profiler.BeginSample("HexVis");
        MeshFilter Filter = GetComponent<MeshFilter>();
        Destroy(Filter.mesh);

        GenerateMesh(Mat);
        Renderer = GetComponent<MeshRenderer>();
        SetSelected(false, false, true);
        SetHovered(false);

        Profiler.EndSample();

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

        if (!MapGenerator.TryGetChunkVis(Location, out ChunkVisualization ChunkVis))
            return;

        ChunkVis?.RefreshTokens();
        GenerateMesh(Mat);
        VisualizeSelection();

        if (!Game.TryGetService(out MiniMap Minimap))
            return;

        Minimap.FillBuffer();
    }

    public void InteractMoveUnit(HexagonVisualization SelectedHex) {
        if (!Game.TryGetServices(out Units UnitService, out Stockpile Stockpile))
            return;

        if (!Game.TryGetService(out MapGenerator MapGenerator))
            return;

        if (!UnitService.TryGetEntityAt(SelectedHex.Location, out TokenizedUnitEntity UnitOnTile))
            return;

        // can't move if there already is a unit!
        if (UnitService.TryGetEntityAt(this.Location, out TokenizedUnitEntity UnitAtTarget))
            return;

        if (!Stockpile.CanAfford(UnitOnTile.GetMovementRequirements()) || UnitOnTile.IsStarving(false))
            return;

        List<Location> Path = Pathfinding.FindPathFromTo(SelectedHex.Location, this.Location);
        int PathCosts = Pathfinding.GetCostsForPath(Path);
        if (Path.Count == 0 || PathCosts > UnitOnTile.RemainingMovement)
            return;

        // step through to trigger revealing etc for every tile instead of teleporting
        for (int i = 1; i < Path.Count; i++)
        {
            if (!MapGenerator.TryGetHexagon(Path[i], out HexagonVisualization TargetHex))
                return;
            int StepCosts = HexagonConfig.GetCostsFromTo(Path[i - 1], Path[i]);

            TargetHex.InteractMoveUnit(UnitOnTile, StepCosts);
        }
    }

    private void InteractMoveUnit(TokenizedUnitEntity Unit, int Costs)
    {
        if (!Game.TryGetServices(out Selectors Selector, out Stockpile Stockpile))
            return;

        // trigger movement range update
        Selector.DeselectHexagon();

        if (!Stockpile.Pay(Unit.GetMovementRequirements()))
            return;

        Unit.MoveTo(this.Location, Costs);

        if (!Generator.TryGetHexagon(Unit.GetLocations().GetMainLocation(), out HexagonVisualization NewHex))
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
        UnitService.TryGetEntityAt(Location, out TokenizedUnitEntity Unit);

        bool bCanPay = Unit != null && Stockpile.CanAfford(Unit.GetMovementRequirements()) && !Unit.IsStarving(false);

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

        HashSet<Location> DirectNeighbours = GetNeighbourTileLocationsInRange(this.Location.ToSet(), true, VisitingRange);
        HashSet<Location> OtherNeighbours = GetNeighbourTileLocationsInRange(this.Location.ToSet(), false, ScoutingRange);
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

    public void VisualizeSelection()
    {
        if (!Renderer)
            return;

        MaterialPropertyBlock Block = new MaterialPropertyBlock();

        Block.SetFloat("_Selected", isSelected ? 1 : 0);
        Block.SetFloat("_Hovered", isHovered ? 1 : 0);
        Block.SetFloat("_Adjacent", isAdjacent || isReachable ? 1 : 0);
        if (Data != null)
        {
            Block.SetFloat("_Malaised", Data.IsMalaised() ? 1 : 0);
            Block.SetFloat("_PreMalaised", Data.IsPreMalaised() ? 1 : 0);
            Block.SetFloat("_Type", HexagonConfig.MaskToInt((int)Data.Type, HexagonConfig.MaxTypeIndex + 1) + 1);
            Block.SetFloat("_Value", Data.DebugValue);
        }

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
