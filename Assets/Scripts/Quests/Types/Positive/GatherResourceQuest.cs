using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/** Quests to gather X more resources */
public class GatherResourceQuest : Quest<Production>
{
    private Statistics Statistics;

    public GatherResourceQuest() : base(){
        Statistics = Game.GetService<Statistics>();
    }

    public override int CheckSuccess(Production Production)
    {
        int Collected = 0;
        foreach (var Tuple in Production.GetTuples())
        {
            Collected += Tuple.Value;
        }

        return Collected;
    }

    public override string GetDescription()
    {
        return "Gather additional resources";
    }
    public override void OnCreated() { }

    public override int GetMaxProgress()
    {
        return Statistics.ResourcesNeeded;
    }

    public override Type GetQuestType()
    {
        return Type.Positive;
    }

    public override Dictionary<IQuestRegister<Production>, ActionList<Production>> GetRegisterMap()
    {
        return new()
        {
            { Game.GetService<Stockpile>(), Stockpile._OnResourcesCollected }
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

    public override void OnAfterCompletion()
    {
        Statistics.IncreaseTarget(ref Statistics.ResourcesNeeded, Statistics.ResourcesIncrease);
    }

    public override bool AreRequirementsFulfilled()
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
