using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

/** DTO for EventCards, useed to minify data to save */
public class EventCardDTO : CardDTO
{
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

    public override byte[] GetData()
    {
        NativeArray<byte> Bytes = SaveGameManager.GetArrayWithBaseFilled(EventCardDTO.GetStaticSize(), base.GetSize(), base.GetData());

        int Pos = base.GetSize();
        Pos = SaveGameManager.AddSaveable(Bytes, Pos, EventData);

        return Bytes.ToArray();
    }

    public override void SetData(NativeArray<byte> Bytes)
    {
        base.SetData(Bytes);
        int Pos = base.GetSize();

        // create random data according to type as we will overwrite it anyway
        SaveGameManager.GetEnumAsByte(Bytes, Pos, out byte bEventType);
        EventData = EventData.CreateRandom((EventData.EventType)bEventType);

        Pos = SaveGameManager.SetSaveable(Bytes, Pos, EventData);
    }
    
    public override int GetSize()
    {
        return GetStaticSize();
    }

    public static new int GetStaticSize()
    {
        return CardDTO.GetStaticSize() + EventData.GetStaticSize();
    }
}
