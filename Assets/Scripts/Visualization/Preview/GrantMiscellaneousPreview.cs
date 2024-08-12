using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static IconFactory;

public class GrantMiscellaneousPreview : IconPreview
{

    public override void Init(Card Card)
    {
        if (Card is not EventCard) 
            return;

        EventCard ECard = Card as EventCard;
        GrantedType = GetMiscTypeFromEventData(ECard.EventData);

        Previewable = ECard.EventData;
        InitRendering();
    }

    public override bool IsFor(Card Card)
    {
        if (Card is not EventCard)
            return false;

        EventCard ECard = Card as EventCard;
        if (ECard.EventData.Type != EventData.EventType.GrantUnit && ECard.EventData.Type != EventData.EventType.RemoveMalaise)
            return false;

        MiscellaneousType OtherGrantedType = GetMiscTypeFromEventData(ECard.EventData);
        return OtherGrantedType == GrantedType;
    }

    private MiscellaneousType GetMiscTypeFromEventData(EventData Data)
    {
        if (Data.Type == EventData.EventType.GrantUnit)
        {
            if (!Game.TryGetService(out IconFactory IconFactory))
                return default;

            GrantUnitEventData UnitEventData = Data as GrantUnitEventData;
            IconFactory.TryGetMiscFromUnit(UnitEventData.GrantedType, out MiscellaneousType MiscType);
            return MiscType;
        }
        if (Data.Type == EventData.EventType.RemoveMalaise)
        {
            return MiscellaneousType.RemoveMalaise;
        }
        return default;
    }


    protected override GameObject CreateVisuals()
    {
        if (!Game.TryGetService(out IconFactory IconFactory))
            return null;

        return IconFactory.GetVisualsForMiscalleneous(GrantedType, null, 1, true);
    }

    protected MiscellaneousType GrantedType;
}
