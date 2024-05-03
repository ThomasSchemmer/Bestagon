using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildingPreviewScreen : ScreenFeatureGroup<MeshPreview>
{
    public MeshPreview BuildingPreview;

    public void Start()
    {
        Init();
        HideFeatures();
        Game.RunAfterServiceInit((IconFactory IconFactory) =>
        {
            BuildingPreview._OnPreviewShown += ShowFeatures;
            BuildingPreview._OnPreviewHidden += HideFeatures;
        });
    }

    public override MeshPreview GetFeatureObject()
    {
        return BuildingPreview;
    }
}
