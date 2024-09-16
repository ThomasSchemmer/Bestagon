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

    public void BroadcastDiscoveryChanged(Unlockables.State State)
    {
        OnDiscoveryChanged.ForEach(_ => _?.Invoke(State));
    }

    public ActionList<Unlockables.State> OnDiscoveryChanged = new();

    public static RelicType CategoryMeadow = RelicType.WoodenMallet | RelicType.Calligulae | RelicType.Cradle;
    public static int MaxIndex = 2;
}

[Flags]
public enum RelicType : uint
{
    DEFAULT = 255,
    Calligulae = 1 << 0,
    WoodenMallet = 1 << 1,
    Cradle = 1 << 2,
}

/** Saveable version of relics, identifiable by type-derived name */
public class RelicDTO : ISaveableData
{

    public Unlockables.State State;
    public RelicType Type;

    public byte[] GetData()
    {
        NativeArray<byte> Bytes = new(GetSize(), Allocator.Temp);
        int Pos = 0;
        Pos = SaveGameManager.AddInt(Bytes, Pos, (int)Type);
        Pos = SaveGameManager.AddByte(Bytes, Pos, (byte)State);

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
        State = (Unlockables.State)DiscoveryByte;
    }

    public static RelicDTO CreateFromRelicEffect(RelicEffect Relic, Unlockables.State State)
    {
        RelicDTO DTO = new();
        DTO.State = State;
        DTO.Type = Relic.Type;
        return DTO;
    }
}
