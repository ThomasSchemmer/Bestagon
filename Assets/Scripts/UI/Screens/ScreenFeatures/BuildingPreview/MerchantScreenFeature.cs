using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MerchantScreenFeature : ScreenFeature<HexagonData>
{
    public Button TradeButton;

    public override bool ShouldBeDisplayed()
    {
        HexagonData Hex = Target.GetFeatureObject();
        if (Hex == null)
            return false;

        if (!Game.TryGetService(out BuildingService Buildings))
            return false;

        if (!Buildings.TryGetBuildingAt(Hex.Location, out BuildingEntity CurrentBuilding))
            return false;

        if (CurrentBuilding.Effect.EffectType != OnTurnBuildingEffect.Type.Merchant)
            return false;

        if (CurrentBuilding.GetWorkingWorkerCount(true) == 0)
            return false;

        return true;
    }

    public override void ShowAt(float YOffset)
    {
        base.ShowAt(YOffset);
        TradeButton.gameObject.SetActive(true);
    }

    public override void Hide()
    {
        base.Hide();
        TradeButton.gameObject.SetActive(false);
    }
}
