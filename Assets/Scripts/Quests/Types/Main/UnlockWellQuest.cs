using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnlockWellQuest : Quest<BuildingConfig.Type>
{

    public UnlockWellQuest() : base() { }

    public override int CheckSuccess(BuildingConfig.Type UnlockedType)
    {
        return UnlockedType == BuildingConfig.Type.Well ? 1 : 0;
    }

    public override Dictionary<IQuestRegister<BuildingConfig.Type>, ActionList<BuildingConfig.Type>> GetRegisterMap()
    {
        if (!Game.TryGetService(out BuildingService Service))
            return new();

        return new() {
            {Service.UnlockableBuildings, Service.UnlockableBuildings._OnTypeChanged }
        };
    }

    public override string GetDescription()
    {
        return "Unlock the well building";
    }

    public override int GetMaxProgress()
    {
        return 1;
    }
    public override void OnCreated() { }

    public override Type GetQuestType()
    {
        return Type.Main;
    }

    public override Sprite GetSprite()
    {
        if (!Game.TryGetService(out IconFactory IconFactory))
            return null;

        return IconFactory.GetIconForMisc(IconFactory.MiscellaneousType.Buildings);
    }

    public override int GetStartProgress()
    {
        if (!Game.TryGetService(out BuildingService BuildingService))
            return 0;

        return !BuildingService.UnlockableBuildings.IsLocked(BuildingConfig.Type.Well) ? 1 : 0;
    }

    public override void OnAfterCompletion()
    {

    }

    public override bool AreRequirementsFulfilled()
    {
        if (!Game.TryGetService(out Units Units))
            return false;

        return Units.HasAnyEntity(UnitEntity.UType.Scout, out TokenizedUnitEntity _);
    }

    public override bool TryGetNextType(out System.Type Type)
    {
        Type = typeof(GatherWaterSkinsQuest);
        return true;
    }
    public override void GrantRewards()
    {
        GrantResources(new Production(Production.Type.Meat, 2));
    }
}
