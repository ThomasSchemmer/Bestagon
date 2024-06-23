using UnityEngine;

public class ProductionPreviewScreenFeature : ScreenFeature<BuildingData>
{
    public override bool ShouldBeDisplayed()
    {
        BuildingData CurrentBuilding = Target.GetFeatureObject();
        return CurrentBuilding != null && Game.TryGetService(out IconFactory IconFactory);
    }

    public override void ShowAt(float YOffset)
    {
        if (!bIsInit)
            return;

        base.ShowAt(YOffset);
        Game.TryGetService(out IconFactory IconFactory);

        BuildingPreviewScreen PreviewScreen = (BuildingPreviewScreen)Target;
        Location PreviewLocation = PreviewScreen.GetPreviewLocation();

        BuildingData CurrentBuilding = Target.GetFeatureObject();
        Production Production = CurrentBuilding.GetProductionPreview(PreviewLocation);
        GameObject Visuals = IconFactory.GetVisualsForProduction(Production, null, false);
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
