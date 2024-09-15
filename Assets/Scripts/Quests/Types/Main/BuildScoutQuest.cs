using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildScoutQuest : Quest<ScriptableEntity>
{
    public BuildScoutQuest() : base() { }

    public override int CheckSuccess(ScriptableEntity Unit)
    {
        if (Unit is not UnitEntity)
            return 0;

        return (Unit as UnitEntity).UnitType == UnitEntity.UType.Scout ? 1 : 0;
    }

    public override Dictionary<IQuestRegister<ScriptableEntity>, ActionList<ScriptableEntity>> GetRegisterMap()
    {
        if (Game.TryGetService(out Units Units))
            return new();

        return new()
        {
            { Game.GetService<Units>().GetRegister(), Units._OnEntityCreated }
        };
    }

    public override string GetDescription()
    {
        return "Recruit a scout to explore the map";
    }

    public override int GetMaxProgress()
    {
        return 1;
    }

    public override Type GetQuestType()
    {
        return Type.Main;
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

    public override void OnCreated() { }

    public override void OnAfterCompletion()
    {

    }

    public override bool AreRequirementsFulfilled()
    {
        return true;
    }

    public override bool TryGetNextType(out System.Type Type)
    {
        Type = typeof(UnlockWellQuest);
        return true;
    }

    public override void GrantRewards()
    {
        GrantResources(new Production(Production.Type.Mushroom, 2));
    }
}
