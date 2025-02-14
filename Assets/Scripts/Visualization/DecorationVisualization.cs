using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class DecorationVisualization : EntityVisualization<DecorationEntity>
{
    public override void Init(DecorationEntity Entity)
    {
        base.Init(Entity);
        if (Entity.DecorationType != DecorationEntity.DType.Amber)
            return;

        gameObject.AddComponent<AmberIndicator>();
    }
}
