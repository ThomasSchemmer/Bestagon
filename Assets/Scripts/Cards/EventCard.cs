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

        GameObject Usages = IconFactory.GetVisualsForMiscalleneous(IconFactory.MiscellaneousType.Usages, 1);
        Usages.transform.SetParent(UsagesTransform, false);

        GameObject EffectObject = GetEventVisuals();
        EffectObject.transform.SetParent(EffectTransform, false);

        CostTransform.gameObject.SetActive(false);
    }

    public GameObject GetEventVisuals()
    {
        return EventData.GetEventVisuals();
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
            case EventData.EventType.GrantUnit: return Obj.AddComponent<UnitPreview>();
            case EventData.EventType.GrantResource: return null;
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
}
