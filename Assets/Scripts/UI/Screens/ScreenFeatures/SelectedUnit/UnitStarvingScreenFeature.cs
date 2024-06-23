using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitStarvingScreenFeature : ScreenFeature<UnitData>
{
    public override bool ShouldBeDisplayed()
    {
        StarvableUnitData Unit = GetTargetAsStarvable();
        return Unit != null && Unit.CurrentFoodCount == 0;
    }

    private StarvableUnitData GetTargetAsStarvable()
    {
        return Target.GetFeatureObject() as StarvableUnitData;
    }


    public override void ShowAt(float YOffset)
    {
        base.ShowAt(YOffset);
        TargetText.color = Color.red;
    }
}
