using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RuinsScreenFeature : ScreenFeature<HexagonData>
{
    public override bool ShouldBeDisplayed()
    {
        return GetFeatureDecoration() != null;
    }

    private DecorationEntity GetFeatureDecoration()
    {
        if (!Game.TryGetService(out DecorationService DecorationService))
            return null;

        HexagonData Feature = Target.GetFeatureObject();
        if (Feature == null)
            return null;

        DecorationService.TryGetEntityAt(Feature.Location, out var Entity);
        return Entity;
    }

    public override void ShowAt(float YOffset, float Height)
    {
        base.ShowAt(YOffset, Height);
        TargetText.text = GetFeatureDecoration().GetDecorationText();
    }

    public override void Hide()
    {
        base.Hide();
        TargetText.text = string.Empty;
    }
}
