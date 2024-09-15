using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RelicIconScreen : SimpleIconScreen
{
    public RelicEffect Relic;

    public void Initialize(RelicEffect Relic)
    {
        this.Relic = Relic;
        Initialize(Relic.Image, Relic.Tooltip, null);
        Relic.OnDiscoveryChanged.Add(OnRelicDiscoveryChanged);

        if (!Game.TryGetService(out RelicService RelicService))
            return;

        OnRelicDiscoveryChanged(RelicService.RelicStatus[Relic.Type]);
    }

    public void OnDestroy()
    {
        if (Relic == null)
            return;

        Relic.OnDiscoveryChanged.Remove(OnRelicDiscoveryChanged);
    }


    public void OnRelicDiscoveryChanged(RelicDiscovery Discovery)
    {
        bool bIsActive = Discovery == RelicDiscovery.Active;
        IconRenderer.color = bIsActive ? ActiveColor : InActiveColor;
    }

    public override void ClickOn(Vector2 PixelPos)
    {
        if (Game.IsIn(Game.GameState.Game))
            return;

        if (!Game.TryGetService(out RelicService RelicService))
            return;

        RelicDiscovery Discovery = RelicService.RelicStatus[Relic.Type];
        Discovery = Discovery == RelicDiscovery.Discovered ? RelicDiscovery.Active : RelicDiscovery.Discovered;
        RelicService.SetRelic(Relic.Type, Discovery);
    }

    public static Color ActiveColor = new Color(1, 1, 1, 1);
    public static Color InActiveColor = new Color(0.25f, 0.25f, 0.25f, 1);
}
