using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildingPreviewScreen : ScreenFeatureGroup<MeshPreview>
{
    public MeshPreview MeshPreview;

    public void Start()
    {
        Init();
        HideFeatures();
        Game.RunAfterServiceInit((IconFactory IconFactory) =>
        {
            MeshPreview._OnPreviewShown += Show;
            MeshPreview._OnPreviewHidden += HideFeatures;
        });
    }

    private void Show()
    {
        if (MeshPreview.CurrentBuilding == null)
            return;

        ShowFeatures();
    }

    public override MeshPreview GetFeatureObject()
    {
        return MeshPreview;
    }
}
