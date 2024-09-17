using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RelicScreen : ScreenUI
{
    public List<RelicIconScreen> Relics = new();

    public Transform RelicContainer;


    public override void Show()
    {
        base.Show();
        ClearRelics();
        Game.RunAfterServiceInit((RelicService RelicService) =>
        {
            InitializeRelics();
        });
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
    }

    private void ClearRelics()
    {
        for (int i = Relics.Count - 1; i >= 0; i--)
        {
            Destroy(Relics[i].gameObject);
        }
        Relics.Clear();
    }
}
