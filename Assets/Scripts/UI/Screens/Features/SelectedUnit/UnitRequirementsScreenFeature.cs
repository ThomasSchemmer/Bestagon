using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitRequirementsScreenFeature : ScreenFeature<UnitData>
{

    RectTransform TextRect;
    RectTransform ProductionRect;

    public override void Init(ScreenFeatureGroup<UnitData> Target)
    {
        base.Init(Target);
        TextRect = transform.GetChild(0).GetComponent<RectTransform>();
        ProductionRect = transform.GetChild(1).GetComponent<RectTransform>();
    }

    public override bool ShouldBeDisplayed()
    {
        ScoutData Scout = GetFeatureObjectAsScout();
        if (Scout == null)
            return false;

        if (Scout.GetMovementRequirements().IsEmpty())
            return false;

        if (!Game.TryGetService(out IconFactory IconFactory))
            return false;

        return true;
    }

    private ScoutData GetFeatureObjectAsScout()
    {
        return (ScoutData)Target.GetFeatureObject();
    }

    public override void ShowAt(float YOffset)
    {
        base.ShowAt(YOffset);
        ScoutData Scout = GetFeatureObjectAsScout();
        Game.TryGetService(out IconFactory IconFactory);

        TextRect.gameObject.SetActive(true);
        ProductionRect.gameObject.SetActive(true);
        Cleanup();

        Production Production = Scout.GetMovementRequirements();
        GameObject Visuals = IconFactory.GetVisualsForProduction(Production);
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
