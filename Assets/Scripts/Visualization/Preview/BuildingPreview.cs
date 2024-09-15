using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildingPreview : MeshPreview
{
    public override void Init(Card Card)
    {
        base.Init(Card);
        if (Card is not BuildingCard)
            return;

        BuildingCard BCard = (BuildingCard)Card;
        Previewable = BCard.GetBuildingData();
        
        InitRendering();
    }

    public override Mesh GetPreviewMesh()
    {
        BuildingEntity BuildingData = GetPreviewableAs<BuildingEntity>();
        if (BuildingData == null)
            return null;

        if (!Game.TryGetService(out MeshFactory MeshFactory))
            return null;

        return MeshFactory.GetMeshFromType(BuildingData.BuildingType);
    }

    public override Material GetPreviewMaterial()
    {
        if (!Game.TryGetService(out PreviewSystem Previews))
            return null;

        return Previews.PreviewMaterial;
    }

    public override bool IsFor(Card Card)
    {
        if (Card is not BuildingCard)
            return false;

        BuildingCard BCard = (BuildingCard)Card;
        return GetPreviewableAs<BuildingEntity>().BuildingType == BCard.GetBuildingData().BuildingType;
    }

}
