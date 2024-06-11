using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BuildingVisualization))]
public class WorkerIndicator : IndicatorComponent
{
    private BuildingVisualization Visualization;

    protected override void Initialize()
    {
        Visualization = GetComponent<BuildingVisualization>();
    }

    protected override int GetIndicatorAmount()
    {
        return Visualization.BuildingData.GetMaximumWorkerCount();
    }

    protected override Sprite GetIndicatorSprite(int i)
    {
        if (!Game.TryGetService(out IconFactory IconFactory))
            return null;

        bool bIsAssigned = Visualization.BuildingData.AssignedWorkers[i] != null;
        IconFactory.MiscellaneousType Type = bIsAssigned ? IconFactory.MiscellaneousType.WorkerIndicator : IconFactory.MiscellaneousType.NoWorkerIndicator;
        return IconFactory.GetIconForMisc(Type);
    }

    protected override Vector3 GetIndicatorWorldPosition(int i)
    {
        Location Location = Visualization.BuildingData.Location;
        return HexagonConfig.TileSpaceToWorldSpace(Location.GlobalTileLocation);
    }

    public override bool IsFor(Location Location)
    {
        return Visualization.BuildingData.Location.Equals(Location);
    }
}
