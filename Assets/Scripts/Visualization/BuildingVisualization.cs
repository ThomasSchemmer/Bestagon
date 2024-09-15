using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildingVisualization : EntityVisualization<BuildingEntity>
{
    public override void Init(BuildingEntity Entity)
    {
        base.Init(Entity);
        gameObject.AddComponent<WorkerIndicator>();
    }
}
