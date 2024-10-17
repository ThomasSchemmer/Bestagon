using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RelicScreen : ScreenUI
{
    public List<RelicIconScreen> Relics = new();

    public Transform RelicContainer;

    protected TMPro.TextMeshProUGUI ActiveText;

    protected override void Initialize()
    {
        base.Initialize();
        ActiveText = Container.transform.GetChild(3).GetComponent<TMPro.TextMeshProUGUI>();

        if (!Game.TryGetService(out RelicService RelicService))
            return;

        RelicService.OnRelicDiscoveryChanged.Add(SetCount);
    }

    public override void Show()
    {
        base.Show();

        ClearRelics();
        Game.RunAfterServiceInit((RelicService RelicService) =>
        {
            InitializeRelics();
        });

        SetCount(RelicType.DEFAULT, Unlockables.State.Locked);
    }

    public override void Hide()
    {
        base.Hide();
        ClearRelics();
    }

    public void OnToggle()
    {
        if (Container == null)
            return;

        if (Container.activeSelf)
        {
            Hide();
        }
        else
        {
            Show();
        }
    }

    public void SetCount(RelicType Type, Unlockables.State State)
    {
        if (!Game.TryGetService(out RelicService RelicService))
            return;

        ActiveText.text = "Active: " + RelicService.CurrentActiveRelics + "/" + RelicService.MaxActiveRelics;
    }

    private void InitializeRelics()
    {
        if (!Game.TryGetService(out RelicService RelicService))
            return;

        List<RelicEffect> Effects = RelicService.Relics.Values.ToList();

        foreach (var Effect in Effects)
        {
            if (RelicService.UnlockableRelics[Effect.Type] == Unlockables.State.Locked)
                continue;

            var RelicIcon = RelicService.CreateRelicIcon(RelicContainer, Effect, false);
            Relics.Add(RelicIcon);
        }
        foreach (var Effect in Effects)
        {
            if (RelicService.UnlockableRelics[Effect.Type] != Unlockables.State.Locked)
                continue;

            var RelicIcon = RelicService.CreateRelicIcon(RelicContainer, Effect, false);
            Relics.Add(RelicIcon);
        }
    }

    private void ClearRelics()
    {
        for (int i = Relics.Count - 1; i >= 0; i--)
        {
            Destroy(Relics[i].gameObject);
        }
        Relics.Clear();
    }

    private void OnDestroy()
    {
        if (!Game.TryGetService(out RelicService RelicService))
            return;

        RelicService.OnRelicDiscoveryChanged.Remove(SetCount);
    }
}
