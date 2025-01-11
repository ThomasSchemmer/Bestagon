using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

/** 
 * Wrapper for all event related info. Also used to create a random event 
 */
public abstract class EventData : ScriptableObject, IPreviewable
{
    public enum EventType
    {
        GrantResource,
        GrantUnit,
        ConvertTile,
        RemoveMalaise
    }

    [SaveableEnum]
    public EventType Type;
    [SaveableEnum]
    public UnitEntity.UType GrantedUnitType;
    [SaveableClass]
    public Production GrantedResource;
    [SaveableEnum]
    public HexagonConfig.HexagonType TargetHexType;

    [SaveableBaseType]
    // Temporary cards will be deleted once played
    public bool bIsTemporary = true;

    public static EventType GetRandomType(int Seed)
    {
        UnityEngine.Random.InitState(Seed);
        return (EventType)(UnityEngine.Random.Range(0, Enum.GetValues(typeof(EventType)).Length));
    }

    public static EventData CreateRandom(EventType Type)
    {
        switch (Type)
        {
            case EventType.GrantUnit: return CreateInstance<GrantUnitEventData>();
            case EventType.GrantResource: return CreateInstance<GrantResourceEventData>();
            case EventType.RemoveMalaise: return CreateInstance<RemoveMalaiseEventData>();
            case EventType.ConvertTile: return CreateInstance<ConvertTileEventData>();
            default: return null;
        }
    }

    public abstract string GetDescription();

    public abstract GameObject GetEventVisuals(ISelectable Parent);

    public abstract string GetEventName();

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

    public bool ShouldBeDeleted()
    {
        return bIsTemporary;
    }

    public abstract Vector3 GetOffset();
    public abstract Quaternion GetRotation();
    public abstract bool IsInteractableWith(HexagonVisualization Hex, bool bIsPreview);
}
