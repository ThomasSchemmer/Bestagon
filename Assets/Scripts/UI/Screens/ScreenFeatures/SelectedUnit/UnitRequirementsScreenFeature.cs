using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/** Displays the resources the unit requires to move, e.g. waterskins for a scout in a desert */
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
        TokenizedUnitEntity ScoutTokenized = GetFeatureObjectAsTokenized();
        if (ScoutTokenized == null)
            return false;

        if (ScoutTokenized.GetMovementRequirements().IsEmpty())
            return false;

        if (!Game.TryGetService(out IconFactory _))
            return false;

        return true;
    }

    private TokenizedUnitEntity GetFeatureObjectAsTokenized()
    {
        return (TokenizedUnitEntity)Target.GetFeatureObject();
    }

    public override void ShowAt(float YOffset, float Height)
    {
        base.ShowAt(YOffset, Height);
        TokenizedUnitEntity Tokenized = GetFeatureObjectAsTokenized();
        Game.TryGetService(out IconFactory IconFactory);

        TextRect.gameObject.SetActive(true);
        ProductionRect.gameObject.SetActive(true);
        Cleanup();

        Production Production = Tokenized.GetMovementRequirements();
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
