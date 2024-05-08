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
    }

    public GameObject GetEventVisuals()
    {
        return EventData.GetEventVisuals();
    }

    public override void InteractWith(HexagonVisualization Hex) {}

    public override bool IsInteractableWith(HexagonVisualization Hex)
    {
        return true;
    }

    public override bool IsPreviewable()
    {
        return false;
    }

    protected override CardCollection GetTargetAfterUse()
    {
        if (!Game.TryGetService(out DiscardDeck Discard))
            return null;

        return Discard;
    }

    protected override void UseInternal() {}
}
