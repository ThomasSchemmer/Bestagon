using System;
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

    public override GameObject GetEventVisuals(ISelectable Parent)
    {
        if (!Game.TryGetService(out IconFactory IconFactory))
            return null;

        return IconFactory.GetVisualsForGrantUnitEffect(this, Parent);
    }

    public override byte[] GetData()
    {
        NativeArray<byte> Bytes = SaveGameManager.GetArrayWithBaseFilled(this, base.GetSize(), base.GetData());

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
        UnitData Unit = CreateUnitData();
        Unit.TryInteractWith(Hex);

        if (!Game.TryGetService(out Selectors Selectors))
            return;

        Selectors.SelectHexagon(Hex);
    }

    public override bool IsPreviewable()
    {
        return true;
    }

    public CardPreview AddEventPreviewByType(GameObject Obj)
    {
        UnitData Unit = GetUnitData();
        if (Unit is TokenizedUnitData)
            return Obj.AddComponent<UnitPreview>();

        return Obj.AddComponent<GrantMiscellaneousPreview>();
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
        if (UnitData == null)
            return Vector3.zero;

        return UnitData.GetOffset();
    }

    public override Quaternion GetRotation()
    {
        TokenizedUnitData UnitData = (GetUnitData() as TokenizedUnitData);
        if (UnitData == null)
            return Quaternion.identity;

        return UnitData.GetRotation();
    }

    public override bool CanBeInteractedOn(HexagonVisualization Hex)
    {
        TokenizedUnitData UnitData = (GetUnitData() as TokenizedUnitData);
        //for now can only be worker, which can always be interacted with
        if (UnitData == null)
            return true;

        return UnitData.CanBeInteractedOn(Hex);
    }

    public override int GetAdjacencyRange()
    {
        return 0;
    }

    public override bool TryGetAdjacencyBonus(out Dictionary<HexagonConfig.HexagonType, Production> Bonus)
    {
        Bonus = GetStandardAdjacencyBonus();
        return true;
    }

    public override bool ShouldShowAdjacency(HexagonVisualization Hex)
    {
        return true;
    }
}
