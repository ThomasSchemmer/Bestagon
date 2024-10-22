using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RelicIconScreen : SimpleIconScreen
{
    public RelicEffect Relic;
    private bool bIsPreview;
    private TMPro.TextMeshProUGUI NameText, DescriptionText;

    public void Initialize(RelicEffect Relic, bool bIsPreview)
    {
        if (!Game.TryGetServices(out IconFactory IconFactory, out RelicService RelicService))
            return;

        bool bIsUnlocked = RelicService.UnlockableRelics[Relic.Type] >= Unlockables.State.Unlocked;
        bIsUnlocked = bIsPreview || bIsUnlocked;
        Sprite Sprite = bIsUnlocked ? Relic.Image : IconFactory.GetIconForMisc(IconFactory.MiscellaneousType.UnknownRelic);
        
        this.Relic = Relic;
        this.bIsPreview = bIsPreview;
        string ToolTip = bIsUnlocked ? Relic.GetEffectDescription() : UnknownText;
        Initialize(Sprite, ToolTip, null);
        Relic.OnDiscoveryChanged.Add(OnRelicDiscoveryChanged);

        if (bIsPreview)
        {
            NameText = transform.GetChild(1).GetComponent<TMPro.TextMeshProUGUI>();
            DescriptionText = transform.GetChild(2).GetComponent<TMPro.TextMeshProUGUI>();

            NameText.text = bIsUnlocked ? Relic.name : UnknownText;
            DescriptionText.text = bIsUnlocked ? Relic.GetEffectDescription() : UnknownText;
        }

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
        bool bIsActive = State == Unlockables.State.Active ||
            State == Unlockables.State.Locked || // locked relics have a hidden icon anyway
            bIsPreview ;
        IconRenderer.color = bIsActive ? ActiveColor : InActiveColor;
    }

    public override void ClickOn(Vector2 PixelPos)
    {
        if (Game.IsIn(Game.GameState.Game))
            return;

        if (!Game.TryGetService(out RelicService RelicService))
            return;

        Unlockables.State State = RelicService.UnlockableRelics[Relic.Type];
        if (State == Unlockables.State.Locked)
            return;

        State = State == Unlockables.State.Unlocked ? Unlockables.State.Active : Unlockables.State.Unlocked;
        RelicService.SetRelic(Relic.Type, State);
    }

    public override bool IsEqual(ISelectable Other)
    {
        RelicIconScreen OtherIcon = Other as RelicIconScreen;
        if (OtherIcon == null)
            return false;

        return Relic.Type == OtherIcon.Relic.Type;
    }

    public static Color ActiveColor = new Color(1, 1, 1, 1);
    public static Color InActiveColor = new Color(0.25f, 0.25f, 0.25f, 1);
    public static string UnknownText = "Unknown Relic";
}
