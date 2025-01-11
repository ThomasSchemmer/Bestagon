using System.Collections;
using System.Collections.Generic;
using Unity.VectorGraphics;
using UnityEngine;

public class EventCard : Card
{
    public EventData EventData;

    public void Init(EventData EventData, CardDTO DTO, int Index) {
        this.EventData = EventData;
        base.Init(DTO, Index);
    }

    public override string GetName()
    {
        return EventData.GetEventName();
    }

    public override bool ShouldBeDeleted()
    {
        return EventData.ShouldBeDeleted();
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

        if (!EventData.bIsTemporary)
            return;

        SVGImage Image = SymbolTransform.GetComponent<SVGImage>();
        Image.sprite = IconFactory.GetIconForMisc(IconFactory.MiscellaneousType.Lightning);
        Image.color = new(1, 1, 1, 1);
    }

    protected override void DeleteVisuals()
    {
        base.DeleteVisuals();
        DeleteVisuals(EffectTransform);
        DeleteVisuals(SymbolTransform);

    }

    public GameObject GetEventVisuals()
    {
        return EventData.GetEventVisuals(this);
    }

    public UnitEntity GetUnitData()
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
        return EventData.IsInteractableWith(Hex, false);
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

    public override LocationSet.AreaSize GetAreaSize()
    {
        return LocationSet.AreaSize.Single;
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
