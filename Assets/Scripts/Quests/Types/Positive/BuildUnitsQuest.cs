using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/** Quests to build X more units */
public class BuildUnitsQuest : Quest<ScriptableEntity>
{
    private Statistics Statistics;

    public BuildUnitsQuest() : base(){
        Statistics = Game.GetService<Statistics>();
    }

    public override int CheckSuccess(ScriptableEntity Item)
    {
        return 1;
    }
    public override void OnCreated() { }

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

    public override Dictionary<IQuestRegister<ScriptableEntity>, ActionList<ScriptableEntity>> GetRegisterMap()
    {
        if (Game.GetService<Units>() == null)
            return new();

        if (Game.GetService<Workers>() == null)
            return new();


        return new()
        {
            { Game.GetService<Units>().GetRegister(), Units._OnEntityCreated },
            { Game.GetService<Workers>().GetRegister(), Workers._OnEntityCreated }
        };
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

    public override bool AreRequirementsFulfilled()
    {
        if (!Game.TryGetService(out Turn Turn))
            return false;

        return Turn.TurnNr > 5;
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
