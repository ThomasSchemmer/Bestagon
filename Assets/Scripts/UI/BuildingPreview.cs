using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildingPreview : MonoBehaviour
{
    void Start()
    {
        Instance = this;
        MeshFilter = GetComponent<MeshFilter>();
        Renderer = GetComponent<MeshRenderer>();
    }

    public static void Hide() {
        if (!Instance)
            return;

        Instance.Renderer.enabled = false;
    }

    public static void Show(Card Card, HexagonVisualization Hex) {
        if (!Instance)
            return;

        Instance._Show(Card, Hex);
    }

    private void _Show(Card Card, HexagonVisualization Hex) {
        Renderer.enabled = true;

        BuildingData Building = Card.GetBuildingData();

        if (Building.GetBuildingType() != CurrentType) {
            BuildingVisualization BuildingVis = BuildingVisualization.CreateFromData(Building);

            MeshFilter.mesh = BuildingVis.GetComponent<MeshFilter>().sharedMesh;
            DestroyImmediate(BuildingVis.gameObject);
        }
        this.transform.position = Hex.transform.position + Building.GetOffset();
        this.transform.localRotation = Building.GetRotation();

        bool Allowed = Building.CanBeBuildOn(Hex);

        MaterialPropertyBlock Block = new MaterialPropertyBlock();
        Block.SetFloat("_Allowed", Allowed ? 1 : 0);
        Renderer.SetPropertyBlock(Block);
    }

    private BuildingData.Type CurrentType = BuildingData.Type.Default;
    private MeshFilter MeshFilter;
    private MeshRenderer Renderer;

    public static BuildingPreview Instance;
}
