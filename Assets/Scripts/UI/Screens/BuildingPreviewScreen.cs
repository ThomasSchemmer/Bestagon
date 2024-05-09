using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildingPreviewScreen : ScreenFeatureGroup<BuildingData>
{
    private PreviewSystem Previews;

    public void Start()
    {
        Init();
        HideFeatures();
        Game.RunAfterServicesInit((IconFactory IconFactory, PreviewSystem Previews) =>
        {
            this.Previews = Previews;
            Previews._OnPreviewShown += Show;
            Previews._OnPreviewHidden += HideFeatures;
        });
    }

    private void Show()
    {
        if (GetFeatureObject() == null)
            return;

        ShowFeatures();
    }

    public override BuildingData GetFeatureObject()
    {
        return Previews.GetPreviewableAs<BuildingData>();
    }
}
