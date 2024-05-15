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
        if (ECard.EventData.Type == EventData.EventType.GrantUnit)
        {
            InitAsUnit(ECard);
        }

        if (ECard.EventData.Type == EventData.EventType.RemoveMalaise)
        {
            InitAsMalaise(ECard);
        }

        Previewable = ECard.EventData;
        InitRendering();
    }

    private void InitAsUnit(EventCard Card)
    {
        if (!Game.TryGetService(out IconFactory IconFactory))
            return;

        GrantUnitEventData UnitEventData = Card.EventData as GrantUnitEventData;
        IconFactory.TryGetMiscFromUnit(UnitEventData.GrantedType, out GrantedType);
    }

    private void InitAsMalaise(EventCard Card)
    {
        GrantedType = MiscellaneousType.RemoveMalaise;
    }


    public override bool IsFor(Card Card)
    {
        if (Card is not EventCard || (Card as EventCard).EventData.Type != EventData.EventType.GrantUnit)
            return false;

        if (!Game.TryGetService(out IconFactory IconFactory))
            return false;

        GrantUnitEventData UnitEventData = (Card as EventCard).EventData as GrantUnitEventData;
        if (!IconFactory.TryGetMiscFromUnit(UnitEventData.GrantedType, out MiscellaneousType OtherGrantedType))
            return false;

        return OtherGrantedType == GrantedType;
    }


    protected override GameObject CreateVisuals()
    {
        if (!Game.TryGetService(out IconFactory IconFactory))
            return null;

        return IconFactory.GetVisualsForMiscalleneous(GrantedType, null, 1);
    }

    protected MiscellaneousType GrantedType;
}
