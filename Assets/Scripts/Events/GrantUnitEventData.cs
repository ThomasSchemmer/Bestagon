using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;


public class GrantUnitEventData : EventData
{
    public GrantUnitEventData()
    {
        Type = EventType.GrantUnit;
    }

    public void OnEnable()
    {
        GrantedUnitType = (UnitEntity.UType)(UnityEngine.Random.Range(0, Enum.GetValues(typeof(UnitEntity.UType)).Length));
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

    public override string GetEventName()
    {
        UnitEntity Unit = GetUnitData();
        return Unit.UnitType.ToString();
    }

    public override void InteractWith(HexagonVisualization Hex)
    {
        UnitEntity Unit = CreateUnitData();
        if (!Unit.TryInteractWith(Hex))
        {
            Destroy(Unit);
            return;
        }

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
        UnitEntity Unit = GetUnitData();
        if (Unit is TokenizedUnitEntity)
            return Obj.AddComponent<UnitPreview>();

        return Obj.AddComponent<GrantMiscellaneousPreview>();
    }

    public UnitEntity CreateUnitData()
    {
        if (!Game.TryGetService(out MeshFactory MeshFactory))
            return null;

        return MeshFactory.CreateDataFromType(GrantedUnitType);
    }

    public UnitEntity GetUnitData()
    {
        if (!Game.TryGetService(out MeshFactory MeshFactory))
            return null;

        return MeshFactory.GetDataFromType(GrantedUnitType);
    }

    public override Vector3 GetOffset()
    {
        TokenizedUnitEntity UnitData = (GetUnitData() as TokenizedUnitEntity);
        if (UnitData == null)
            return Vector3.zero;

        return UnitData.GetOffset();
    }

    public override Quaternion GetRotation()
    {
        TokenizedUnitEntity UnitData = (GetUnitData() as TokenizedUnitEntity);
        if (UnitData == null)
            return Quaternion.identity;

        return UnitData.GetRotation();
    }

    public override bool IsPreviewInteractableWith(HexagonVisualization Hex, bool bIsPreview)
    {
        TokenizedUnitEntity UnitData = (GetUnitData() as TokenizedUnitEntity);
        //for now can only be worker, which can always be interacted with
        if (UnitData == null)
            return true;

        return UnitData.IsPreviewInteractableWith(Hex, bIsPreview);
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
