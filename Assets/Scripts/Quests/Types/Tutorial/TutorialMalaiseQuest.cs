using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static TutorialSystem;

public class TutorialMalaiseQuest : Quest<int>
{
    public TutorialMalaiseQuest() : base()
    {
    }

    public override int CheckSuccess(int Target)
    {
        return 1;
    }

    public override string GetDescription()
    {
        return "Witness the malaise by ending your turn";
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
        return new()
        {
            { Game.GetService<Turn>(), Turn._OnTurnEnded }
        };
    }

    public override Sprite GetSprite()
    {
        if (!Game.TryGetService(out IconFactory IconFactory))
            return null;

        return IconFactory.GetIconForMisc(IconFactory.MiscellaneousType.RemoveMalaise);
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

        MalaiseData.SpreadInitially(SpawnLocation);
        TutorialSystem.DisplayTextFor(TutorialType.Malaise);
    }

    public override bool AreRequirementsFulfilled()
    {
        return true;
    }

    public override bool TryGetNextType(out System.Type Type)
    {
        Type = typeof(TutorialAbandonRunQuest);
        return true;
    }

    public override bool ShouldUnlockDirectly()
    {
        return true;
    }

    public override void GrantRewards()
    {
        if (!Game.TryGetService(out Stockpile Stockpile))
            return;

        Stockpile.AddUpgrades(1);
    }

    protected Location SpawnLocation = new(0, 0, 0, 0);
}
