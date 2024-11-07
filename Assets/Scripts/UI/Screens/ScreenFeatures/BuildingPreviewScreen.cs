using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * UI info that shows more data about the building the player might be going to build
 * Needs to be hooked to preview, as Card only triggers on actually selecting a card - 
 * not every time the cursor has hovered another hex!
 */
public class BuildingPreviewScreen : ScreenFeatureGroup<BuildingEntity>
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

    public override BuildingEntity GetFeatureObject()
    {
        return Previews.GetPreviewableAs<BuildingEntity>();
    }

    public Location GetPreviewLocation()
    {
        if (Previews == null)
            return Location.Zero;

        return Previews.GetPreviewLocation();
    }

    public override bool HasFeatureObject()
    {
        if (Previews == null)
            return false;

        return Previews.HasPreviewableAs<BuildingEntity>();
    }
}
