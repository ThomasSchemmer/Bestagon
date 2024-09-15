using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitNameScreenFeature : ScreenFeature<UnitEntity>
{
    public override bool ShouldBeDisplayed()
    {
        return GetFeatureObjectAsScout() != null;
    }

    private ScoutEntity GetFeatureObjectAsScout()
    {
        return (ScoutEntity)Target.GetFeatureObject();
    }

    public override void ShowAt(float YOffset)
    {
        base.ShowAt(YOffset);
        ScoutEntity SelectedUnit = GetFeatureObjectAsScout();
        TargetText.text = SelectedUnit.GetName();
    }

    public override void Hide()
    {
        base.Hide();
        TargetText.text = string.Empty;
    }

    public override void Init(ScreenFeatureGroup<UnitEntity> Target)
    {
        base.Init(Target);
    }
}
