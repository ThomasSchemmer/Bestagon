using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProductionLabelScreenFeature : ScreenFeature<BuildingEntity>
{
    public override bool ShouldBeDisplayed()
    {
        if (!Target.HasFeatureObject())
            return false;

        BuildingEntity CurrentBuilding = Target.GetFeatureObject();
        return CurrentBuilding != null && Game.TryGetService(out IconFactory IconFactory);
    }

}
