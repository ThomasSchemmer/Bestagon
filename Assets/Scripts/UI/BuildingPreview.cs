using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildingPreview : GameService
{
    public void Hide() {
        if (!IsInit)
            return;

        Renderer.enabled = false;
        CurrentLocation = null;
        CurrentBuilding = null;
        _OnPreviewHidden?.Invoke();
    }

    public void Show(BuildingCard Card, HexagonVisualization Hex) {
        if (!IsInit)
            return;

        Renderer.enabled = true;

        CurrentBuilding = Card.GetBuildingData();
        CurrentLocation = Hex.Location;

        if (CurrentBuilding.BuildingType != CurrentType) {
            BuildingVisualization BuildingVis = BuildingVisualization.CreateFromData(CurrentBuilding);

            MeshFilter.mesh = BuildingVis.GetComponent<MeshFilter>().sharedMesh;
            DestroyImmediate(BuildingVis.gameObject);
        }
        this.transform.position = Hex.transform.position + CurrentBuilding.GetOffset();
        this.transform.localRotation = CurrentBuilding.GetRotation();

        bool Allowed = CurrentBuilding.CanBeBuildOn(Hex);

        MaterialPropertyBlock Block = new MaterialPropertyBlock();
        Block.SetFloat("_Allowed", Allowed ? 1 : 0);
        Renderer.SetPropertyBlock(Block);
        _OnPreviewShown?.Invoke();
    }

    protected override void StartServiceInternal()
    {
        MeshFilter = GetComponent<MeshFilter>();
        Renderer = GetComponent<MeshRenderer>();
        _OnInit?.Invoke();
    }

    protected override void StopServiceInternal() {}

    public Location CurrentLocation;
    public BuildingData CurrentBuilding;

    private BuildingConfig.Type CurrentType = BuildingConfig.Type.DEFAULT;
    private MeshFilter MeshFilter;
    private MeshRenderer Renderer;

    public delegate void OnPreviewShown();
    public delegate void OnPreviewHidden();
    public OnPreviewShown _OnPreviewShown;
    public OnPreviewHidden _OnPreviewHidden;
}
