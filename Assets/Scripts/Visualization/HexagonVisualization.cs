﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using Unity.Mathematics;
using System.Threading;

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

        MapGenerator = Game.GetService<MapGenerator>();
        UpdateMesh();
        Data._OnDiscovery = UpdateMesh;
        ChunkData.Visualization?.FinishVisualization();
    }

    void UpdateMesh()
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
        if (Card) {
            InteractBuildBuilding(Card);
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
        if (!Game.TryGetService(out Units UnitService))
            return;

        if (!UnitService.TryGetUnitsAt(SelectedHex.Location, out List<UnitData> UnitsOnTile))
            return;

        UnitData Unit = UnitsOnTile[0];
        List<Location> Path = Pathfinding.FindPathFromTo(SelectedHex.Location, this.Location);
        int PathCosts = Pathfinding.GetCostsForPath(Path);
        if (Path.Count == 0 || PathCosts > Unit.RemainingMovement)
            return;

        InteractMoveUnit(Unit, PathCosts);
    }

    private void InteractMoveUnit(UnitData Unit, int Costs)
    {
        if (!Game.TryGetService(out Selector Selector))
            return;

        // trigger movement range update
        Selector.DeselectHexagon();

        // update both chunks where the worker was and is going to
        if (!MapGenerator.TryGetChunkData(Unit.Location, out ChunkData Chunk))
            return;

        Chunk.Visualization.RefreshTokens();

        Unit.MoveTo(this.Location, Costs);

        if (!MapGenerator.TryGetChunkData(Unit.Location, out Chunk))
            return;

        Chunk.Visualization.RefreshTokens();

        if (!MapGenerator.TryGetHexagon(Unit.Location, out HexagonVisualization NewHex))
            return;

        Selector.SelectHexagon(NewHex);
    }

    private void InteractBuildBuilding(Card Card)
    {
        if (!Game.TryGetServices(out Selector Selector, out Stockpile Stockpile))
            return;

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
        Selector.SelectHexagon(this);

        Card.Use();
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

    private void UpdateBuildingPreview()
    {
        if (!Game.TryGetService(out Selector Selector))
            return;

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

    public void ShowReachableLocations(bool bShow)
    {
        if (!Game.TryGetService(out Units UnitService))
            return;

        UnitService.TryGetUnitsAt(Location, out List<UnitData> UnitsOnTile);
        UnitData Unit = UnitsOnTile.Count > 0 ? UnitsOnTile[0] : null;
        bool bIsVisible = Unit != null && bShow;
        int Range = Unit != null ? Unit.RemainingMovement : 0;

        // check for each reachable tile if it should be highlighted
        HashSet<Location> ReachableLocations = Pathfinding.FindReachableLocationsFrom(Location, Range);

        foreach (Location ReachableLocation in ReachableLocations) {
            if (!MapGenerator.TryGetHexagon(ReachableLocation, out HexagonVisualization ReachableHex))
                continue;

            ReachableHex.isReachable = bIsVisible;
            ReachableHex.VisualizeSelection();
        }
    }

    public void UpdateDiscoveryState(int VisitingRange, int ScoutingRange)
    {
        if (!Game.TryGetService(out MapGenerator MapGenerator))
            return;

        HashSet<Location> DirectNeighbours = MapGenerator.GetNeighbourTileLocationsInRange(this.Location, VisitingRange);
        HashSet<Location> OtherNeighbours = MapGenerator.GetNeighbourTileLocationsInRange(this.Location, ScoutingRange);
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

    public Location Location;
    public ChunkData Chunk;
    public HexagonData Data;

    protected bool isHovered, isSelected, isAdjacent, isReachable, isPathway;

    protected Vector3[] Vertices;
    protected int[] Triangles;

    private MeshRenderer Renderer;
    private MapGenerator MapGenerator;
    private Material Mat;
}
