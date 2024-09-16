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

        OnRelicDiscoveryChanged(RelicService.UnlockableRelics[Relic.Type]);
    }

    public void OnDestroy()
    {
        if (Relic == null)
            return;

        Relic.OnDiscoveryChanged.Remove(OnRelicDiscoveryChanged);
    }


    public void OnRelicDiscoveryChanged(Unlockables.State State)
    {
        bool bIsActive = State == Unlockables.State.Active;
        IconRenderer.color = bIsActive ? ActiveColor : InActiveColor;
    }

    public override void ClickOn(Vector2 PixelPos)
    {
        if (Game.IsIn(Game.GameState.Game))
            return;

        if (!Game.TryGetService(out RelicService RelicService))
            return;

        Unlockables.State State = RelicService.UnlockableRelics[Relic.Type];
        State = State == Unlockables.State.Unlocked ? Unlockables.State.Active : Unlockables.State.Unlocked;
        RelicService.SetRelic(Relic.Type, State);
    }

    public static Color ActiveColor = new Color(1, 1, 1, 1);
    public static Color InActiveColor = new Color(0.25f, 0.25f, 0.25f, 1);
}
