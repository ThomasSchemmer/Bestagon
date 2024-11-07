using UnityEngine;

public class ProductionPreviewScreenFeature : ScreenFeature<BuildingEntity>
{
    public override bool ShouldBeDisplayed()
    {
        if (!Target.HasFeatureObject())
            return false;

        if (!Game.TryGetServices(out IconFactory _, out Selectors _))
            return false;

        return Target.GetFeatureObject() != null;
    }

    public override void ShowAt(float YOffset)
    {
        if (!bIsInit)
            return;

        Game.TryGetServices(out IconFactory IconFactory, out Selectors Selectors);

        var Hex = Selectors.GetHoveredHexagon();
        if (Hex == null)
            return;

        Location PreviewLocation = Hex.Location;

        BuildingEntity CurrentBuilding = Target.GetFeatureObject();
        if (!LocationSet.TryGetAround(PreviewLocation, CurrentBuilding.Area, out var Area))
            return;

        base.ShowAt(YOffset);
        Production Production = CurrentBuilding.GetProductionPreview(Area);
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
