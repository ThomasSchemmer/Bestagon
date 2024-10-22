using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RelicGroupScreen : MonoBehaviour
{
    public Transform RelicContainer;
    public TMPro.TextMeshProUGUI FlavorText;

    [HideInInspector]
    public List<RelicIconScreen> Relics = new();

    public void InitializeRelics(SerializedDictionary<RelicType, Unlockables.State> Category, int Tier)
    {
        if (!Game.TryGetServices(out RelicService RelicService, out IconFactory IconFactory))
            return;

        Tier++;
        FlavorText.text = "Tier " + Tier;

        foreach (var RelicType in Category.Keys)
        {
            if (RelicService.UnlockableRelics[RelicType] == Unlockables.State.Locked)
                continue;

            var RelicIcon = IconFactory.CreateRelicIcon(RelicContainer, RelicService.Relics[RelicType], false);
            Relics.Add(RelicIcon);
        }
        foreach (var RelicType in Category.Keys)
        {
            if (RelicService.UnlockableRelics[RelicType] != Unlockables.State.Locked)
                continue;

            var RelicIcon = IconFactory.CreateRelicIcon(RelicContainer, RelicService.Relics[RelicType], false);
            Relics.Add(RelicIcon);
        }
    }
}
