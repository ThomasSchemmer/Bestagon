using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProductionPreviewScreenFeature : ScreenFeature<MeshPreview>
{
    public override bool ShouldBeDisplayed()
    {
        MeshPreview Preview = Target.GetFeatureObject();
        return Preview.CurrentBuilding != null && Game.TryGetService(out IconFactory IconFactory);
    }

    public override void ShowAt(float YOffset)
    {
        if (!bIsInit)
            return;

        base.ShowAt(YOffset);
        Game.TryGetService(out IconFactory IconFactory);

        MeshPreview Preview = Target.GetFeatureObject();
        Production Production = Preview.CurrentBuilding.GetProductionPreview(Preview.CurrentLocation);
        GameObject Visuals = IconFactory.GetVisualsForProduction(Production);
        Visuals.transform.SetParent(TargetTransform, false);
    }

    public override void Hide()
    {
        if (!bIsInit)
            return;

        base.Hide();

        if (TargetTransform.childCount == 0)
            return;

        Destroy(TargetTransform.GetChild(0).gameObject);
    }
}
