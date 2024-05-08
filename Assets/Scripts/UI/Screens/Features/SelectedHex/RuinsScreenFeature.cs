using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RuinsScreenFeature : ScreenFeature<HexagonData>
{
    public override bool ShouldBeDisplayed()
    {
        return Target.GetFeatureObject()?.Decoration != HexagonConfig.HexagonDecoration.None;
    }

    public override void ShowAt(float YOffset)
    {
        base.ShowAt(YOffset);
        TargetText.text = Target.GetFeatureObject().GetDecorationText();
    }

    public override void Hide()
    {
        base.Hide();
        TargetText.text = string.Empty;
    }
}
