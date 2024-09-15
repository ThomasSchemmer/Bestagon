using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildingTypeScreenFeature : ScreenFeature<HexagonData>
{
    public override bool ShouldBeDisplayed()
    {
        return TryGetBuilding(out BuildingEntity BuildingData);
    }

    private bool TryGetBuilding(out BuildingEntity Building)
    {
        Building = null;
        HexagonData SelectedHex = Target.GetFeatureObject();
        if (SelectedHex == null)
            return false;

        if (!Game.TryGetService(out BuildingService Buildings))
            return false;

        if (!Buildings.TryGetBuildingAt(SelectedHex.Location, out Building))
            return false;

        return true;
    }

    public override void ShowAt(float YOffset)
    {
        base.ShowAt(YOffset);

        TryGetBuilding(out BuildingEntity BuildingData);
        TargetText.text = BuildingData.BuildingType.ToString();
    }

    public override void Hide()
    {
        base.Hide();
        TargetText.text = string.Empty;
    }
}
