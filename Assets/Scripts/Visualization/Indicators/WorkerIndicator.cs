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

        bool bIsAssigned = Visualization.Entity.AssignedWorkers[i] != null;
        IconFactory.MiscellaneousType Type = bIsAssigned ? IconFactory.MiscellaneousType.WorkerIndicator : IconFactory.MiscellaneousType.NoWorkerIndicator;
        return IconFactory.GetIconForMisc(Type);
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

    protected override void OnDestroy()
    {
        base.OnDestroy();
        Workers._OnWorkersAssigned -= OnWorkerChanged;
    }
    public override bool NeedsVisualUpdate()
    {
        // static image: only updates when a worker is changed, which already triggers a callback
        return false;
    }
}
