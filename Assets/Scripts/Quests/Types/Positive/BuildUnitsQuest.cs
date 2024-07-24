using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/** Quests to build X more units */
public class BuildUnitsQuest : Quest<TokenizedUnitData>
{
    private Statistics Statistics;

    public BuildUnitsQuest() : base(){
        Statistics = Game.GetService<Statistics>();
    }

    public override int CheckSuccess(TokenizedUnitData Item)
    {
        return 1;
    }

    public override string GetDescription()
    {
        return "Build additional units";
    }

    public override int GetMaxProgress()
    {
        return Statistics.UnitsNeeded;
    }

    public override Type GetQuestType()
    {
        return Type.Positive;
    }

    public override IQuestRegister<TokenizedUnitData> GetRegistrar()
    {
        return Game.GetService<Units>();
    }
    public override ActionList<TokenizedUnitData> GetDelegates()
    {
        return Units._OnUnitCreated;
    }

    public override Sprite GetSprite()
    {
        if (!Game.TryGetService(out IconFactory IconFactory))
            return null;

        return IconFactory.GetIconForMisc(IconFactory.MiscellaneousType.Scout);
    }

    public override int GetStartProgress()
    {
        return 0;
    }

    public override void OnAfterCompletion()
    {
        Statistics.IncreaseTarget(ref Statistics.UnitsNeeded, Statistics.UnitsIncrease);
    }

    public override bool ShouldUnlock()
    {
        if (!Game.TryGetService(out Turn Turn))
            return false;

        return Turn.TurnNr > 3;
    }

    public override bool TryGetNextType(out System.Type Type)
    {
        Type = GetType();
        return true;
    }

    public override void GrantRewards()
    {
        GrantUpgradePoints(1);
    }
}
