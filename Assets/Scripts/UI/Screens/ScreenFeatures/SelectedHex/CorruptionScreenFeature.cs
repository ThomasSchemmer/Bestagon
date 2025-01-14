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

        return SelectedHex.IsMalaised() || SelectedHex.IsPreMalaised();
    }

    public override void ShowAt(float YOffset, float Height)
    {
        base.ShowAt(YOffset, Height);
        HexagonData Hex = Target.GetFeatureObject();
        TargetText.text =
            Hex.GetState(HexagonData.State.PreMalaised) ? "Will be corrupted" :
            Hex.GetState(HexagonData.State.Malaised) ? "Corrupted" : 
            "";
    }

    public override void Hide()
    {
        base.Hide();
        TargetText.text = string.Empty;
    }

}
