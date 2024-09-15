using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/** Quest to build X more buildings */
public class BuildBuildingsQuest : Quest<BuildingEntity>
{
    private Statistics Statistics;

    public BuildBuildingsQuest() : base(){
        Statistics = Game.GetService<Statistics>();
    }

    public override int CheckSuccess(BuildingEntity Building)
    {
        return 1;
    }
    public override void OnCreated() { }

    public override string GetDescription()
    {
        return "Build additional buildings";
    }

    public override int GetMaxProgress()
    {
        return Statistics.BuildingsNeeded;
    }

    public override Type GetQuestType()
    {
        return Type.Positive;
    }

    public override Dictionary<IQuestRegister<BuildingEntity>, ActionList<BuildingEntity>> GetRegisterMap()
    {
        if (Game.GetService<BuildingService>() == null)
            return new();

        return new()
        {
            { Game.GetService<BuildingService>(), BuildingService._OnBuildingBuilt }
        };
    }

    public override Sprite GetSprite()
    {
        if (!Game.TryGetService(out IconFactory IconFactory))
            return null;

        return IconFactory.GetIconForMisc(IconFactory.MiscellaneousType.Buildings);
    }

    public override int GetStartProgress()
    {
        return 0;
    }

    public override void OnAfterCompletion()
    {
        Statistics.IncreaseTarget(ref Statistics.BuildingsNeeded, Statistics.BuildingsIncrease);
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
        GrantUpgradePoints(1);
    }
}
