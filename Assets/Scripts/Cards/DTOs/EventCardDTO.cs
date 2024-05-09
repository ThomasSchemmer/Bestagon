using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

/** Similar to @UnitCardDTO the saving is a bit complicated due to its different sizes */
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
        // since we have to write the overall size at the first byte, we have to move all base data 
        NativeArray<byte> Bytes = new(GetSize(), Allocator.Temp);
        int Pos = 0;
        Pos = SaveGameManager.AddInt(Bytes, Pos, GetSize());

        NativeSlice<byte> Slice = new NativeSlice<byte>(Bytes, Pos, base.GetSize());
        Slice.CopyFrom(base.GetData());
        Pos += base.GetSize();

        Pos = SaveGameManager.AddSaveable(Bytes, Pos, EventData);

        return Bytes.ToArray();
    }

    public override void SetData(NativeArray<byte> Bytes)
    {
        // skip the first "size" byte, as the savegamemanager already has handled it
        int Pos = sizeof(int);

        // now move the base data before we can set the actual data
        NativeSlice<byte> Slice = new NativeSlice<byte>(Bytes, Pos, base.GetSize());
        Slice.CopyFrom(base.GetData());

        // initialize the base DTO, aka card type
        NativeArray<byte> BaseBytes = new(base.GetSize(), Allocator.Temp);
        BaseBytes.CopyFrom(Slice.ToArray());
        base.SetData(BaseBytes);
        Pos += base.GetSize();

        // create random data according to type as we will overwrite it anyway
        SaveGameManager.GetEnumAsByte(Bytes, Pos, out byte bEventType);
        EventData = EventData.CreateRandom((EventData.EventType)bEventType);

        Pos = SaveGameManager.SetSaveable(Bytes, Pos, EventData);
    }
    public override bool ShouldLoadWithLoadedSize() { return true; }

    public override int GetSize()
    {
        return GetStaticSize(EventData.Type);
    }

    public static int GetStaticSize(EventData.EventType Type)
    {
        int EventSize = 0;
        switch (Type)
        {
            case EventData.EventType.GrantUnit: return GrantUnitEventData.GetStaticSize();
            case EventData.EventType.GrantResource: return GrantResourceEventData.GetStaticSize();
            default: break;
        }

        return GetStaticSize() + EventSize + sizeof(int);
    }
}
