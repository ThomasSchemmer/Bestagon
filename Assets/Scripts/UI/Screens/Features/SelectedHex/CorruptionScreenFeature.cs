using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CorruptionScreenFeature : ScreenFeature<HexagonData>
{
    public override bool ShouldBeDisplayed()
    {
        HexagonData SelectedHex = Target.GetFeatureObject();
        if (SelectedHex == null)
            return false;

        return SelectedHex.IsMalaised();
    }

    public override void ShowAt(float YOffset)
    {
        base.ShowAt(YOffset);
        TargetText.text = "Corrupted";
    }

    public override void Hide()
    {
        base.Hide();
        TargetText.text = string.Empty;
    }

}
