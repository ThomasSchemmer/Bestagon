using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static TutorialSystem;

public class TutorialTileQuest : Quest<HexagonVisualization>
{
    public TutorialTileQuest() : base()
    {
    }

    public override int CheckSuccess(HexagonVisualization Target)
    {
        return 1;
    }

    public override string GetDescription()
    {
        return "Select a tile";
    }

    public override int GetMaxProgress()
    {
        return 1;
    }

    public override Type GetQuestType()
    {
        return Type.Positive;
    }

    public override Dictionary<IQuestRegister<HexagonVisualization>, ActionList<HexagonVisualization>> GetRegisterMap()
    {
        if (!Game.TryGetService(out Selectors Selectors))
            return default;

        if (Selectors.HexagonSelector == null)
            return default;

        return new()
        {
            { Selectors.HexagonSelector, Selectors.HexagonSelector._OnSelected }
        };
    }

    public override Sprite GetSprite()
    {
        if (!Game.TryGetService(out IconFactory IconFactory))
            return null;

        return IconFactory.GetIconForMisc(IconFactory.MiscellaneousType.Tile);
    }

    public override int GetStartProgress()
    {
        return 0;
    }

    public override void OnAfterCompletion() { }

    public override void OnCreated()
    {
        if (!Game.TryGetService(out TutorialSystem TutorialSystem))
            return;

        TutorialSystem.DisplayTextFor(TutorialType.Tile);
    }

    public override bool AreRequirementsFulfilled()
    {
        return true;
    }

    public override bool TryGetNextType(out System.Type Type)
    {
        Type = typeof(TutorialResourceQuest);
        return true;
    }

    public override bool ShouldUnlockDirectly()
    {
        return true;
    }

    public override void GrantRewards()
    {
    }
}
