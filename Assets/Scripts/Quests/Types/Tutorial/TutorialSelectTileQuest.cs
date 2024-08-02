using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static TutorialSystem;

public class TutorialSelectTileQuest : Quest<HexagonVisualization>
{
    public TutorialSelectTileQuest() : base()
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

    public override IQuestRegister<HexagonVisualization> GetRegistrar()
    {
        return Game.GetService<Selectors>().HexagonSelector;
    }

    public override ActionList<HexagonVisualization> GetDelegates()
    {
        if (!Game.TryGetService(out Selectors Selectors))
            return default;

        return Selectors.HexagonSelector._OnSelected;
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
        Type = GetType();
        return true;
    }

    public override void GrantRewards()
    {
    }
}
