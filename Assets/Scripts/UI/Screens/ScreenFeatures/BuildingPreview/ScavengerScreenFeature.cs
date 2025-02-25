using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class ScavengerScreenFeature : ScreenFeature<HexagonData>
{
    public Button TradeButton;
    public ScavengerScreen ScavengerScreen;

    public override bool ShouldBeDisplayed()
    {
        HexagonData Hex = Target.GetFeatureObject();
        if (Hex == null)
            return false;

        if (!Game.TryGetService(out BuildingService Buildings))
            return false;

        if (!Buildings.TryGetEntityAt(Hex.Location, out BuildingEntity CurrentBuilding))
            return false;

        if (CurrentBuilding.Effect.EffectType != OnTurnBuildingEffect.Type.Scavenger)
            return false;

        if (CurrentBuilding.GetWorkingWorkerCount(true) == 0)
            return false;

        return true;
    }

    public override void ShowAt(float YOffset, float Height)
    {
        base.ShowAt(YOffset, Height);
        TradeButton.gameObject.SetActive(true);
    }

    public void OpenScavengerUI()
    {
        HexagonData Hex = Target.GetFeatureObject();
        if (Hex == null)
            return;

        ScavengerScreen.Show(Hex.Location);
    }

    public override void Hide()
    {
        base.Hide();
        TradeButton.gameObject.SetActive(false);
    }
}
