using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshPreview : GameService
{
    public enum PreviewType
    {
        None,
        Building,
        Unit
    }

    public void Hide() {
        if (!IsInit)
            return;

        Reset();
        Renderer.enabled = false;
        _OnPreviewHidden?.Invoke();
    }

    private void Reset()
    {
        CurrentType = PreviewType.None;
        OldType = PreviewType.None;
        CurrentIndex = -1;
        OldIndex = -1;
        CurrentLocation = null;
        CurrentBuilding = null;
        CurrentUnit = null;
    }

    public void Show(Card Card, HexagonVisualization Hex) {
        if (!IsInit)
            return;

        Renderer.enabled = true;

        CurrentLocation = Hex.Location;
        SetCurrentValues(Card);
        HandleMeshUpdate();

        IPreviewable Preview = GetPreviewable();
        if (Preview == null)
        {
            Hide();
            return;
        }

        transform.position = Hex.transform.position + Preview.GetOffset();
        transform.localRotation = Preview.GetRotation();

        bool Allowed = Preview.CanBeInteractedOn(Hex);

        MaterialPropertyBlock Block = new MaterialPropertyBlock();
        Block.SetFloat("_Allowed", Allowed ? 1 : 0);
        Renderer.SetPropertyBlock(Block);
        _OnPreviewShown?.Invoke();
    }

    private void HandleMeshUpdate()
    {
        if (OldType == CurrentType && OldIndex == CurrentIndex) 
            return;

        if (!Game.TryGetService(out MeshFactory MeshFactory))
            return;

        switch (CurrentType)
        {
            case PreviewType.None: MeshFilter.sharedMesh = null; break;
            case PreviewType.Unit: MeshFilter.sharedMesh = MeshFactory.GetMeshFromType((UnitData.UnitType)CurrentIndex); break;
            case PreviewType.Building: MeshFilter.sharedMesh = MeshFactory.GetMeshFromType((BuildingConfig.Type)CurrentIndex); break;
        }
        OldType = CurrentType;
        OldIndex = CurrentIndex;
    }

    private IPreviewable GetPreviewable()
    {
        switch (CurrentType)
        {
            case PreviewType.Building: return CurrentBuilding;
            case PreviewType.Unit: return CurrentUnit;
            default: return null;
        }
    }

    private void SetCurrentValues(Card Card)
    {
        if (Card is BuildingCard)
        {
            CurrentType = PreviewType.Building;
            CurrentBuilding = (Card as BuildingCard).GetBuildingData();
            CurrentIndex = (int)CurrentBuilding.BuildingType;
            CurrentUnit = null;
        }
        else if (Card is UnitCard)
        {
            CurrentType = PreviewType.Unit;
            CurrentUnit = (Card as UnitCard).Unit;
            CurrentIndex = (int)CurrentUnit.Type;
            CurrentBuilding = null;
        }
        else
        {
            CurrentType = PreviewType.None;
            CurrentIndex = -1;
            CurrentUnit = null;
            CurrentBuilding = null;
        }
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
    public TokenizedUnitData CurrentUnit;

    private PreviewType CurrentType = PreviewType.None;
    private PreviewType OldType = PreviewType.None;
    private int CurrentIndex = -1;
    private int OldIndex = -1;
    private MeshFilter MeshFilter;
    private MeshRenderer Renderer;

    public delegate void OnPreviewShown();
    public delegate void OnPreviewHidden();
    public OnPreviewShown _OnPreviewShown;
    public OnPreviewHidden _OnPreviewHidden;
}
