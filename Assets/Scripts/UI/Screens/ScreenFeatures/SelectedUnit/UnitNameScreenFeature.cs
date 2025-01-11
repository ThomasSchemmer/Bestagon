using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/** Displays the name of the currently selected unit */
public class UnitNameScreenFeature : ScreenFeature<UnitEntity>
{
    public override bool ShouldBeDisplayed()
    {
        return GetFeatureObjectAsToken() != null;
    }

    private TokenizedUnitEntity GetFeatureObjectAsToken()
    {
        return (TokenizedUnitEntity)Target.GetFeatureObject();
    }

    public override void ShowAt(float YOffset)
    {
        base.ShowAt(YOffset);
        TokenizedUnitEntity TokenizedUnit = GetFeatureObjectAsToken();
        TargetText.text = TokenizedUnit.GetName();
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
