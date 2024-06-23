using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitNameScreenFeature : ScreenFeature<UnitData>
{
    public override bool ShouldBeDisplayed()
    {
        return GetFeatureObjectAsScout() != null;
    }

    private ScoutData GetFeatureObjectAsScout()
    {
        return (ScoutData)Target.GetFeatureObject();
    }

    public override void ShowAt(float YOffset)
    {
        base.ShowAt(YOffset);
        ScoutData SelectedUnit = GetFeatureObjectAsScout();
        TargetText.text = SelectedUnit.GetName();
    }

    public override void Hide()
    {
        base.Hide();
        TargetText.text = string.Empty;
    }

    public override void Init(ScreenFeatureGroup<UnitData> Target)
    {
        base.Init(Target);
    }
}
