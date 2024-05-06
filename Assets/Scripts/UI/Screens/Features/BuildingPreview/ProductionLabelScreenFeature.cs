using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProductionLabelScreenFeature : ScreenFeature<MeshPreview>
{
    public override bool ShouldBeDisplayed()
    {
        MeshPreview Preview = Target.GetFeatureObject();
        return Preview.CurrentBuilding != null && Game.TryGetService(out IconFactory IconFactory);
    }

}
