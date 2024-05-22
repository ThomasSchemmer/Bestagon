using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConvertTilePreview : IconPreview
{
    public override void Init(Card Card)
    {
        if (Card is not EventCard)
            return;

        EventCard ECard = Card as EventCard;
        if (ECard.EventData.Type != EventData.EventType.ConvertTile)
            return;

        InitAsConvertTile(ECard);
        Previewable = ECard.EventData;
        InitRendering();
    }

    private void InitAsConvertTile(EventCard EventCard)
    {
        GrantedType = (EventCard.EventData as ConvertTileEventData).TargetType;
    }

    public override bool IsFor(Card Card)
    {
        if (Card is not EventCard || (Card as EventCard).EventData.Type != EventData.EventType.ConvertTile)
            return false;

        HexagonConfig.HexagonType OtherType = ((Card as EventCard).EventData as ConvertTileEventData).TargetType;
        return GrantedType == OtherType;
    }
    protected override GameObject CreateVisuals()
    {
        if (!Game.TryGetService(out IconFactory IconFactory))
            return null;

        return IconFactory.GetVisualsForHexTypes(GrantedType, null);
    }

    protected HexagonConfig.HexagonType GrantedType;
}
