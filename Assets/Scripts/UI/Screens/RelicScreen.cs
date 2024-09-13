using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RelicScreen : ScreenUI
{
    public List<RelicIconScreen> Relics = new();

    public GameObject RelicIconScreenPrefab;
    public Transform RelicContainer;

    protected override void Initialize()
    {
        base.Initialize();
        Game.RunAfterServiceInit((RelicService RelicService) =>
        {
            InitializeRelics();
        });
    }

    private void InitializeRelics()
    {
        if (!Game.TryGetService(out RelicService RelicService))
            return;

        List<GameplayEffect> Effects = RelicService.GetPlayerBehavior().GetActiveEffects();

        foreach (var Effect in Effects)
        {
            if (Effect is not RelicEffect)
                continue;

            GameObject GO = Instantiate(RelicIconScreenPrefab);
            RelicIconScreen RelicIcon = GO.GetComponent<RelicIconScreen>();
            RelicEffect Relic = Effect as RelicEffect;
            RelicIcon.Initialize(Relic);
            RelicIcon.transform.SetParent(RelicContainer, false);
            Relics.Add(RelicIcon);
        }
    }
}
