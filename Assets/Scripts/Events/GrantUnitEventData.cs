using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;


public class GrantUnitEventData : EventData
{
    public UnitData.UnitType GrantedType;

    public GrantUnitEventData()
    {
        Type = EventType.GrantUnit;
    }

    public void OnEnable()
    {
        GrantedType = (UnitData.UnitType)(UnityEngine.Random.Range(0, Enum.GetValues(typeof(UnitData.UnitType)).Length));
    }

    public override string GetDescription()
    {
        return "Grants this unit for free";
    }

    public override GameObject GetEventVisuals()
    {
        if (!Game.TryGetService(out IconFactory IconFactory))
            return null;

        return IconFactory.GetVisualsForGrantUnitEffect(this);
    }

    public override byte[] GetData()
    {
        NativeArray<byte> Bytes = SaveGameManager.GetArrayWithBaseFilled(this, GetSize(), base.GetData());

        int Pos = base.GetSize();
        Pos = SaveGameManager.AddEnumAsByte(Bytes, Pos, (byte)GrantedType);

        return Bytes.ToArray();
    }

    public override int GetSize()
    {
        return GetStaticSize();
    }

    public static new int GetStaticSize()
    {
        return EventData.GetStaticSize() + sizeof(byte);
    }

    public override void SetData(NativeArray<byte> Bytes)
    {
        base.SetData(Bytes);
        int Pos = base.GetSize();
        Pos = SaveGameManager.GetEnumAsByte(Bytes, Pos, out byte bGrantedType);

        GrantedType = (UnitData.UnitType)bGrantedType;
    }
}
