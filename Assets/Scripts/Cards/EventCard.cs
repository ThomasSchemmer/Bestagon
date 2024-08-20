using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventCard : Card
{
    public EventData EventData;

    public void Init(EventData EventData, int Index) {
        this.EventData = EventData;
        base.Init(Index);
    }

    public override string GetName()
    {
        return EventData.GetEventName();
    }

    public override bool ShouldBeDeleted()
    {
        return EventData.bIsTemporary;
    }

    protected override void GenerateVisuals()
    {
        base.GenerateVisuals();

        if (!Game.TryGetService(out IconFactory IconFactory))
            return;

        GameObject Usages = IconFactory.GetVisualsForMiscalleneous(IconFactory.MiscellaneousType.Usages, this, 1);
        Usages.transform.SetParent(UsagesTransform, false);

        GameObject EffectObject = GetEventVisuals();
        EffectObject.transform.SetParent(EffectTransform, false);

        CostTransform.gameObject.SetActive(false);
    }

    protected override void DeleteVisuals()
    {
        base.DeleteVisuals();
        DeleteVisuals(EffectTransform);

    }

    public GameObject GetEventVisuals()
    {
        return EventData.GetEventVisuals(this);
    }

    public UnitData GetUnitData()
    {
        if (EventData.Type != EventData.EventType.GrantUnit)
            return null;

        return (EventData as GrantUnitEventData).GetUnitData();
    }

    public CardPreview AddPreviewByType(GameObject Obj)
    {
        switch (EventData.Type)
        {
            case EventData.EventType.GrantUnit: return (EventData as GrantUnitEventData).AddEventPreviewByType(Obj);
            case EventData.EventType.GrantResource: return Obj.AddComponent<GrantResourcesPreview>();
            case EventData.EventType.RemoveMalaise: return Obj.AddComponent<GrantMiscellaneousPreview>();
            case EventData.EventType.ConvertTile: return Obj.AddComponent<ConvertTilePreview>();
            default: return null;
        }
    }

    public override bool IsCardInteractableWith(HexagonVisualization Hex)
    {
        return EventData.IsPreviewInteractableWith(Hex, false);
    }

    public override void InteractWith(HexagonVisualization Hex) {
        EventData.InteractWith(Hex);
        Use();
    }

    public override bool IsPreviewable()
    {
        return EventData.IsPreviewable();
    }

    protected override CardCollection GetTargetAfterUse()
    {
        // events are one time only in most cases
        if (!Game.TryGetService(out CardStash CardStash))
            return null;

        return CardStash;
    }

    protected override void UseInternal() {
        bWasUsedUp = true;
    }

    public override int GetAdjacencyRange()
    {
        return EventData.GetAdjacencyRange();
    }

    public override bool TryGetAdjacencyBonus(out Dictionary<HexagonConfig.HexagonType, Production> Bonus)
    {
        return EventData.TryGetAdjacencyBonus(out Bonus);
    }

    public override bool ShouldShowAdjacency(HexagonVisualization Hex)
    {
        return EventData.ShouldShowAdjacency(Hex);
    }

    public override bool IsCustomRuleApplying(Location NeighbourLocation)
    {
        return false;
    }

    public override bool CanBeUpgraded()
    {
        return false;
    }
}
