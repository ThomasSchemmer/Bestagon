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

    public override void ShowAt(float YOffset)
    {
        base.ShowAt(YOffset);
        HexagonData.MalaiseState State = Target.GetFeatureObject().MalaisedState;
        switch (State)
        {
            case HexagonData.MalaiseState.None: TargetText.text = ""; break;
            case HexagonData.MalaiseState.PreMalaise: TargetText.text = "Will be corrupted"; break;
            case HexagonData.MalaiseState.Malaised: TargetText.text = "Corrupted"; break;
        }
    }

    public override void Hide()
    {
        base.Hide();
        TargetText.text = string.Empty;
    }

}
