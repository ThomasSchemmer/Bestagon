using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitPreview : MeshPreview
{
    public override void Init(Card Card)
    {
        base.Init(Card);
        if (Card is not EventCard || (Card as EventCard).EventData.Type != EventData.EventType.GrantUnit)
            return;

        EventCard ECard = (EventCard)Card;
        Previewable = ECard.EventData;

        InitRendering();
    }

    public override Mesh GetPreviewMesh()
    {
        GrantUnitEventData UnitData = GetPreviewableAs<GrantUnitEventData>();
        if (UnitData == null)
            return null;

        if (!Game.TryGetService(out MeshFactory MeshFactory))
            return null;

        return MeshFactory.GetMeshFromType(UnitData.GrantedUnitType);
    }

    public override Material GetPreviewMaterial()
    {
        if (!Game.TryGetService(out PreviewSystem Previews))
            return null;

        return Previews.PreviewMaterial;
    }

    public override bool IsFor(Card Card)
    {
        if (Card is not EventCard || (Card as EventCard).EventData.Type != EventData.EventType.GrantUnit)
            return false;

        EventCard OtherCard = (EventCard)Card;
        return GetPreviewableAs<GrantUnitEventData>().GrantedUnitType == (OtherCard.EventData as GrantUnitEventData).GrantedUnitType;
    }
}
