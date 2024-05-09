using System;
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

    public override bool IsInteractableWith(HexagonVisualization Hex)
    {
        return true;
    }

    public override void InteractWith(HexagonVisualization Hex)
    {
        if (!Game.TryGetServices(out Selectors Selector, out Units Units))
            return;

        UnitData Unit = CreateUnitData();
        if (!Unit.TryInteractWith(Hex))
            return;

        Selector.ForceDeselect();
        Selector.SelectHexagon(Hex);
    }

    public override bool IsPreviewable()
    {
        return (GetUnitData() as TokenizedUnitData) != null;
    }

    public UnitData CreateUnitData()
    {
        if (!Game.TryGetService(out MeshFactory MeshFactory))
            return null;

        return MeshFactory.CreateDataFromType(GrantedType);
    }

    public UnitData GetUnitData()
    {
        if (!Game.TryGetService(out MeshFactory MeshFactory))
            return null;

        return MeshFactory.GetDataFromType(GrantedType);
    }

    public override Vector3 GetOffset()
    {
        TokenizedUnitData UnitData = (GetUnitData() as TokenizedUnitData);
        return UnitData.GetOffset();
    }

    public override Quaternion GetRotation()
    {
        TokenizedUnitData UnitData = (GetUnitData() as TokenizedUnitData);
        return UnitData.GetRotation();
    }

    public override bool CanBeInteractedOn(HexagonVisualization Hex)
    {
        TokenizedUnitData UnitData = (GetUnitData() as TokenizedUnitData);
        return UnitData.CanBeInteractedOn(Hex);
    }
}
