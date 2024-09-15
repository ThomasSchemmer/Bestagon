using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitStarvingScreenFeature : ScreenFeature<UnitEntity>
{
    public override bool ShouldBeDisplayed()
    {
        StarvableUnitEntity Unit = GetTargetAsStarvable();
        return Unit != null && Unit.CurrentFoodCount == 0;
    }

    private StarvableUnitEntity GetTargetAsStarvable()
    {
        return Target.GetFeatureObject() as StarvableUnitEntity;
    }


    public override void ShowAt(float YOffset)
    {
        base.ShowAt(YOffset);
        TargetText.color = Color.red;
    }
}
