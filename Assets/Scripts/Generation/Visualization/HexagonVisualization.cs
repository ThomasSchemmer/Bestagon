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
        SetSelected(false);
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

    public void SetSelected(bool Selected) {
        isSelected = Selected;
        VisualizeSelection();
        UpdatePreview();
    }

    public void SetHovered(bool Hovered) {
        isHovered = Hovered;
        VisualizeSelection();
        UpdatePreview();
    }

    public void Interact() {
        Card Card = Selector.GetSelectedCard();
        if (!Card)
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
        CardHand.DiscardCard(Card);
    }

    public void BuildBuildingFromCard(BuildingData Building) {
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

    private void UpdatePreview() {
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

    public void VisualizeSelection() {
        MaterialPropertyBlock Block = new MaterialPropertyBlock();
        Block.SetFloat("_Selected", isSelected ? 1 : 0);
        Block.SetFloat("_Hovered", isHovered ? 1 : 0);
        Block.SetFloat("_Adjacent", isAdjacent ? 1 : 0);
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

    protected bool isHovered, isSelected, isAdjacent;

    protected Vector3[] Vertices;
    protected int[] Triangles;

    private MeshRenderer Renderer;
}
