using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static HexagonData;

/** Quests to scout X more tiles */
public class ScoutTilesQuest : Quest<DiscoveryState>
{
    private Statistics Statistics;

    public ScoutTilesQuest() : base(){
        Statistics = Game.GetService<Statistics>();
    }

    public override int CheckSuccess(DiscoveryState State)
    {
        return State < DiscoveryState.Visited ? 0 : 1;
    }

    public override string GetDescription()
    {
        return "Reveal additional tiles";
    }

    public override int GetMaxProgress()
    {
        return Statistics.MovesNeeded;
    }

    public override Type GetQuestType()
    {
        return Type.Positive;
    }

    public override IQuestRegister<DiscoveryState> GetRegistrar()
    {
        return Game.GetService<MapGenerator>();
    }

    public override List<Action<DiscoveryState>> GetDelegates()
    {
        return MapGenerator._OnDiscoveredTile;
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
        Statistics.IncreaseTarget(ref Statistics.MovesNeeded, Statistics.MovesIncrease);
    }

    public override bool ShouldUnlock()
    {
        if (!Game.TryGetServices(out Units Units, out Turn Turn))
            return false;

        return Units.HasAnyUnit(UnitData.UnitType.Scout, out TokenizedUnitData _) && Turn.TurnNr > 1;
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
