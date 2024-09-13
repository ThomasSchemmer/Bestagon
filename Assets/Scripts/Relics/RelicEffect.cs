using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

/** 
 * Scriptable for Relics, used solely for defining. 
 * Cannot be stored in savegame itself, convert to DTO
 */ 
[CreateAssetMenu(fileName = "Relic", menuName = "ScriptableObjects/Relic", order = 10)]
public class RelicEffect : GameplayEffect
{
    public RelicType Type;
    public Sprite Image;
    public string Tooltip;

    public void BroadcastDiscoveryChanged(RelicDiscovery Discovery)
    {
        OnDiscoveryChanged.ForEach(_ => _?.Invoke(Discovery));
    }

    public ActionList<RelicDiscovery> OnDiscoveryChanged = new();
}

public enum RelicType : uint
{
    Calligulae = 0,
    WoodenMallet = 1,
    Cradle = 2,
}

public enum RelicDiscovery : uint
{
    Unknown = 1 << 0,
    Discovered = 1 << 1,
    Active = 1 << 2
}

/** Saveable version of relics, identifiable by type-derived name */
public class RelicDTO : ISaveableData
{

    public RelicDiscovery Disc;
    public RelicType Type;

    public byte[] GetData()
    {
        NativeArray<byte> Bytes = new(GetSize(), Allocator.Temp);
        int Pos = 0;
        Pos = SaveGameManager.AddInt(Bytes, Pos, (int)Type);
        Pos = SaveGameManager.AddByte(Bytes, Pos, (byte)Disc);

        return Bytes.ToArray();
    }

    public int GetSize()
    {
        return GetStaticSize();
    }

    public static int GetStaticSize()
    {
        // typee and discoverey
        return sizeof(uint) + 1;
    }

    public void SetData(NativeArray<byte> Bytes)
    {
        int Pos = 0;
        Pos = SaveGameManager.GetInt(Bytes, Pos, out int iType);
        Pos = SaveGameManager.GetByte(Bytes, Pos, out byte DiscoveryByte);
        Type = (RelicType)iType;
        Disc = (RelicDiscovery)DiscoveryByte;
    }

    public static RelicDTO CreateFromRelicEffect(RelicEffect Relic, RelicDiscovery Disc)
    {
        RelicDTO DTO = new();
        DTO.Disc = Disc;
        DTO.Type = Relic.Type;
        return DTO;
    }
}
