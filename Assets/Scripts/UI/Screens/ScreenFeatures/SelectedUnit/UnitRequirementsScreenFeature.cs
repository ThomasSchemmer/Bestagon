using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitRequirementsScreenFeature : ScreenFeature<UnitEntity>
{

    RectTransform TextRect;
    RectTransform ProductionRect;

    public override void Init(ScreenFeatureGroup<UnitEntity> Target)
    {
        base.Init(Target);
        TextRect = transform.GetChild(0).GetComponent<RectTransform>();
        ProductionRect = transform.GetChild(1).GetComponent<RectTransform>();
    }

    public override bool ShouldBeDisplayed()
    {
        ScoutEntity Scout = GetFeatureObjectAsScout();
        if (Scout == null)
            return false;

        if (Scout.GetMovementRequirements().IsEmpty())
            return false;

        if (!Game.TryGetService(out IconFactory IconFactory))
            return false;

        return true;
    }

    private ScoutEntity GetFeatureObjectAsScout()
    {
        return (ScoutEntity)Target.GetFeatureObject();
    }

    public override void ShowAt(float YOffset)
    {
        base.ShowAt(YOffset);
        ScoutEntity Scout = GetFeatureObjectAsScout();
        Game.TryGetService(out IconFactory IconFactory);

        TextRect.gameObject.SetActive(true);
        ProductionRect.gameObject.SetActive(true);
        Cleanup();

        Production Production = Scout.GetMovementRequirements();
        GameObject Visuals = IconFactory.GetVisualsForProduction(Production, null, true);
        Visuals.transform.SetParent(ProductionRect, false);
    }

    private void Cleanup()
    {
        if (ProductionRect.childCount == 0)
            return;

        Destroy(ProductionRect.GetChild(0).gameObject);
    }
    public override void Hide()
    {
        base.Hide();

        TextRect.gameObject.SetActive(false);
        ProductionRect.gameObject.SetActive(false);
        Cleanup();
    }
}
