using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrantResourcesPreview : IconPreview
{
    public override void Init(Card Card)
    {
        if (Card is not EventCard || (Card as EventCard).EventData.Type != EventData.EventType.GrantResource)
            return;

        Resources = ((Card as EventCard).EventData as GrantResourceEventData).GrantedResource;
        Previewable = (Card as EventCard).EventData;
        InitRendering();
    }

    public override bool IsFor(Card Card)
    {
        if (Card is not EventCard || (Card as EventCard).EventData.Type != EventData.EventType.GrantResource)
            return false;

        Production OtherResources = ((Card as EventCard).EventData as GrantResourceEventData).GrantedResource;
        return Resources.Equals(OtherResources);
    }

    protected override GameObject CreateVisuals()
    {
        if (!Game.TryGetService(out IconFactory IconFactory))
            return null;

        return IconFactory.GetVisualsForProduction(Resources, null, false);
    }

    protected Production Resources;
}
