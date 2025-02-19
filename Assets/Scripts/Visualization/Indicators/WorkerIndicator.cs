using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BuildingVisualization))]
public class WorkerIndicator : SpriteIndicatorComponent
{
    private BuildingVisualization Visualization;

    protected override void Initialize()
    {
        Visualization = GetComponent<BuildingVisualization>();
        Workers._OnWorkersAssigned += OnWorkerChanged;
        StarvableUnitEntity._OnUnitStarving += OnWorkerStarving;
    }

    protected override int GetTargetLayer()
    {
        return LayerMask.NameToLayer("UI");
    }

    protected override int GetIndicatorAmount()
    {
        return Visualization.Entity.GetMaximumWorkerCount();
    }

    protected override Sprite GetIndicatorSprite(int i)
    {
        if (!Game.TryGetService(out IconFactory IconFactory))
            return null;

        bool bShowHunger = Settings.Get()[SettingName.ShowHungerIndicators].Value > 0;
        GetWorkerStatus(i, out var bIsAssigned, out var bIsStarving);
        IconFactory.MiscellaneousType Type = 
            bIsStarving && bShowHunger ? IconFactory.MiscellaneousType.Hunger :
            bIsAssigned ? IconFactory.MiscellaneousType.WorkerIndicator : 
            IconFactory.MiscellaneousType.NoWorkerIndicator;
        return IconFactory.GetIconForMisc(Type);
    }

    private void GetWorkerStatus(int i, out bool bIsAssigned, out bool bIsStarving)
    {
        WorkerEntity Worker = Visualization.Entity.AssignedWorkers[i];
        bIsAssigned = Worker != null;
        bIsStarving = bIsAssigned && Worker.IsStarving(true);
    }

    protected override void ApplyIndicatorScreenPosition(int i, RectTransform IndicatorTransform)
    {
        Location Location = Visualization.Entity.GetLocations().GetMainLocation();
        Vector3 WorldPos = HexagonConfig.TileSpaceToWorldSpace(Location.GlobalTileLocation);
        IndicatorTransform.position = Service.WorldPosToScreenPos(WorldPos);
        IndicatorTransform.position += GetIndicatorScreenOffset(i);
    }

    public override bool IsFor(LocationSet Location)
    {
        return Visualization.Entity.GetLocations().Equals(Location);
    }

    public void OnWorkerChanged(LocationSet Locations)
    {
        if (!IsFor(Locations))
            return;

        UpdateIndicatorVisuals();
    }

    public void OnWorkerStarving(StarvableUnitEntity Unit, bool bIsStarving)
    {
        if (Unit is not WorkerEntity Worker)
            return;

        BuildingEntity Building = Worker.GetAssignedBuilding();
        if (Building == null)
            return;

        if (!IsFor(Building.GetLocations()))
            return;

        UpdateIndicatorVisuals();
    }

    protected override void UpdateIndicatorVisuals(int i)
    {
        base.UpdateIndicatorVisuals(i);
        int Scale = GetIconScale();
        Indicators[i].sizeDelta = Vector2.one * GetWidth();
    }

    protected override int GetWidth()
    {
        return base.GetWidth() + GetIconScale();
    }

    private int GetIconScale()
    {
        return Settings.Get()[SettingName.WorkerUIScale].Value * SizeInc;
    }

    protected override int GetOffset()
    {
        return base.GetOffset() + GetIconScale();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        Workers._OnWorkersAssigned -= OnWorkerChanged;
        StarvableUnitEntity._OnUnitStarving -= OnWorkerStarving;
    }

    public override bool NeedsVisualUpdate()
    {
        // static image: only updates when a worker is changed, which already triggers a callback
        return false;
    }

    public static int SizeInc = 5;
}
