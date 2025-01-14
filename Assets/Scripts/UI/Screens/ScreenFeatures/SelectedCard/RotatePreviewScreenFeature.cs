using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RotatePreviewScreenFeature : ScreenFeature<BuildingCard>
{
    public Button RotateButton;

    public override bool ShouldBeDisplayed()
    {
        if (!Target.HasFeatureObject())
            return false;

        BuildingEntity CurrentBuilding = Target.GetFeatureObject().GetBuildingData();
        return CurrentBuilding != null && CurrentBuilding.Area != LocationSet.AreaSize.Single;
    }

    public override void ShowAt(float YOffset, float Height)
    {
        base.ShowAt(YOffset, Height);
        RotateButton.gameObject.SetActive(true);
    }

    public override void Hide()
    {
        base.Hide();
        RotateButton.gameObject.SetActive(false);
    }

    public void RotatePreview()
    {
        if (!Game.TryGetService(out PreviewSystem Previews))
            return;

        var Preview = Previews.GetPreviewAs<BuildingPreview>();
        if (Preview == null)
            return;

        Preview.RotatePreview();
    }
}
