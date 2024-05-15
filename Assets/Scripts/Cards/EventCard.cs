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
        return EventData.Type.ToString();
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
            default: return null;
        }
    }


    public override void InteractWith(HexagonVisualization Hex) {
        EventData.InteractWith(Hex);
        Use();
    }

    public override bool IsInteractableWith(HexagonVisualization Hex)
    {
        return EventData.IsInteractableWith(Hex);
    }

    public override bool IsPreviewable()
    {
        return EventData.IsPreviewable();
    }

    protected override CardCollection GetTargetAfterUse()
    {
        if (!Game.TryGetService(out DiscardDeck Discard))
            return null;

        return Discard;
    }

    protected override void UseInternal() {}

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
}