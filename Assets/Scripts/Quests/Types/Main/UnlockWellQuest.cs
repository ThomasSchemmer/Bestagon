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

    public override ActionList<BuildingConfig.Type> GetDelegates()
    {
        return Unlockables._OnUnlock;
    }

    public override string GetDescription()
    {
        return "Unlock the well building";
    }

    public override int GetMaxProgress()
    {
        return 1;
    }

    public override Type GetQuestType()
    {
        return Type.Main;
    }

    public override IQuestRegister<BuildingConfig.Type> GetRegistrar()
    {
        return Game.GetService<Unlockables>();
    }

    public override Sprite GetSprite()
    {
        if (!Game.TryGetService(out IconFactory IconFactory))
            return null;

        return IconFactory.GetIconForMisc(IconFactory.MiscellaneousType.Buildings);
    }

    public override int GetStartProgress()
    {
        if (!Game.TryGetService(out Unlockables Unlockables))
            return 0;

        return !Unlockables.IsLocked(BuildingConfig.Type.Well) ? 1 : 0;
    }

    public override void OnAfterCompletion()
    {

    }

    public override bool ShouldUnlock()
    {
        if (!Game.TryGetService(out Units Units))
            return false;

        return Units.HasAnyUnit(UnitData.UnitType.Scout, out TokenizedUnitData _);
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
