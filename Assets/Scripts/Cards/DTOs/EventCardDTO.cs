using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

/** DTO for EventCards, useed to minify data to save */
public class EventCardDTO : CardDTO
{
    [SaveableClass]
    public EventData EventData;

    public EventCardDTO(Card Card)
    {
        EventCard EventCard = Card as EventCard;
        if (EventCard == null)
            return;

        EventData = EventCard.EventData;
    }

    public EventCardDTO() {}

    public static EventCardDTO CreateFromEventData(EventData EventData)
    {
        EventCardDTO DTO = new();
        DTO.EventData = EventData;
        return DTO;
    }

    public override Type GetCardType()
    {
        return Type.Event;
    }
        
    public override bool ShouldBeDeleted()
    {
        return EventData.ShouldBeDeleted();
    }
}
