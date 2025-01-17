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
        int MaxCount = Enum.GetValues(typeof(UnitEntity.UType)).Length;
        bool bIsHarbourLocked = true;
        if (Game.TryGetService(out BuildingService Buildings))
        {
            bIsHarbourLocked = Buildings.UnlockableBuildings.IsLocked(BuildingConfig.Type.Harbour);
        }
        int Count = bIsHarbourLocked ? MaxCount - 1 : MaxCount;
        GrantedUnitType = (UnitEntity.UType)(UnityEngine.Random.Range(0, Count));
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

    public override bool InteractWith(HexagonVisualization Hex)
    {
        if (!Game.TryGetService(out Selectors Selectors))
            return false;
        if (!Game.TryGetServices(out Workers Workers, out Units Units))
            return false;

        EntityProvider Provider = GrantedUnitType == UnitEntity.UType.Worker ? Workers : Units;

        if (!Provider.TryCreateNewEntity((int)GrantedUnitType, Hex.Location.ToSet()))
            return false;

        Selectors.SelectHexagon(Hex);
        return true;
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

    public override bool IsInteractableWith(HexagonVisualization Hex, bool bIsPreview)
    {
        TokenizedUnitEntity UnitData = (GetUnitData() as TokenizedUnitEntity);
        //for now can only be worker, which can always be interacted with
        if (UnitData == null)
            return true;

        return UnitData.IsInteractableWith(Hex, bIsPreview);
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
