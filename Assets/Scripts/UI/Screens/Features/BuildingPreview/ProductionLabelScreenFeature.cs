using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProductionLabelScreenFeature : ScreenFeature<BuildingData>
{
    public override bool ShouldBeDisplayed()
    {
        BuildingData CurrentBuilding = Target.GetFeatureObject();
        return CurrentBuilding != null && Game.TryGetService(out IconFactory IconFactory);
    }

}
