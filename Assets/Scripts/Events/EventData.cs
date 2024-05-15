using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

/** 
 * Wrapper for all event related info. Also used to create a random event 
 */
public abstract class EventData : ScriptableObject, ISaveable, IPreviewable
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
        return EventType.RemoveMalaise;
        return (EventType)(UnityEngine.Random.Range(0, Enum.GetValues(typeof(EventType)).Length));
    }

    public static EventData CreateRandom(EventType Type)
    {
        switch (Type)
        {
            case EventType.GrantUnit: return CreateInstance<GrantUnitEventData>();
            case EventType.GrantResource: return CreateInstance<GrantResourceEventData>();
            case EventType.RemoveMalaise: return CreateInstance<RemoveMalaiseEventData>();
            default: return null;
        }
    }

    public virtual int GetSize()
    {
        return GetStaticSize();
    }

    public abstract string GetDescription();

    public abstract GameObject GetEventVisuals(ISelectable Parent);

    public abstract bool IsInteractableWith(HexagonVisualization Hex);

    public abstract void InteractWith(HexagonVisualization Hex);

    public abstract bool IsPreviewable(); 
    
    public abstract int GetAdjacencyRange();
    public abstract bool TryGetAdjacencyBonus(out Dictionary<HexagonConfig.HexagonType, Production> Bonus);

    public abstract bool ShouldShowAdjacency(HexagonVisualization Hex);

    protected Dictionary<HexagonConfig.HexagonType, Production> GetStandardAdjacencyBonus()
    {
        Dictionary<HexagonConfig.HexagonType, Production> Bonus = new();
        var Types = Enum.GetValues(typeof(HexagonConfig.HexagonType));
        foreach (HexagonConfig.HexagonType Type in Types)
        {
            Bonus.Add(Type, Production.Empty);
        }
        return Bonus;
    }



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

    public abstract Vector3 GetOffset();
    public abstract Quaternion GetRotation();
    public abstract bool CanBeInteractedOn(HexagonVisualization Hex);
}