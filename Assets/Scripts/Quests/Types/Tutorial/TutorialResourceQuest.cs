using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static TutorialSystem;

public class TutorialResourceQuest : Quest<int>
{
    public TutorialResourceQuest() : base()
    {
    }

    public override int CheckSuccess(int Nr)
    {
        return 1;
    }

    public override string GetDescription()
    {
        return "Click on a resource category";
    }

    public override int GetMaxProgress()
    {
        return 1;
    }

    public override Type GetQuestType()
    {
        return Type.Positive;
    }

    public override Dictionary<IQuestRegister<int>, ActionList<int>> GetRegisterMap()
    {
        if (Game.GetService<Stockpile>() == null)
            return new();

        return new()
        {
            { Game.GetService<Stockpile>(), Stockpile._OnResourceCategorySelected }
        };
    }

    public override Sprite GetSprite()
    {
        if (!Game.TryGetService(out IconFactory IconFactory))
            return null;

        return IconFactory.GetIconForMisc(IconFactory.MiscellaneousType.Usages);
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

        TutorialSystem.DisplayTextFor(TutorialType.Resources);
    }

    public override bool AreRequirementsFulfilled()
    {
        return true;
    }

    public override bool TryGetNextType(out System.Type Type)
    {
        Type = typeof(TutorialCardsQuest);
        return true;
    }

    public override void GrantRewards()
    {
    }

    public override bool ShouldUnlockDirectly()
    {
        return true;
    }
}
