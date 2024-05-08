using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

/** 
 * Wrapper for all event related info. Also used to create a random event 
 */
public abstract class EventData : ScriptableObject, ISaveable
{
    public enum EventType
    {
        GrantResource,
        GrantUnit,
        ConvertTile,
        RemoveMalaise
    }

    public EventType Type;

    public static EventData CreateRandom()
    {
        EventType Type = GetRandomType();
        return CreateRandom(Type);
    }

    public static EventType GetRandomType()
    {
        // todo: debug remove after all types are implemented
        return EventType.GrantUnit;
        return (EventType)(UnityEngine.Random.Range(0, Enum.GetValues(typeof(EventType)).Length));
    }

    public static EventData CreateRandom(EventType Type)
    {
        switch (Type)
        {
            case EventType.GrantUnit: return CreateInstance<GrantUnitEventData>();
            default: return null;
        }
    }

    public virtual int GetSize()
    {
        return GetStaticSize();
    }

    public abstract string GetDescription();

    public abstract GameObject GetEventVisuals();


    public static int GetStaticSize()
    {
        return sizeof(byte);
    }

    public virtual byte[] GetData()
    {
        NativeArray<byte> Bytes = new(GetSize(), Allocator.Temp);
        int Pos = 0;
        Pos = SaveGameManager.AddEnumAsByte(Bytes, Pos, (byte)Type);

        return Bytes.ToArray();
    }

    public virtual void SetData(NativeArray<byte> Bytes)
    {
        int Pos = 0;
        Pos = SaveGameManager.GetEnumAsByte(Bytes, Pos, out byte bType);

        Type = (EventType)bType;
    }
}
