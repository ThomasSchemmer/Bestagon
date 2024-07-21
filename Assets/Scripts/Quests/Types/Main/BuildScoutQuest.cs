using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildScoutQuest : Quest<TokenizedUnitData>
{
    public BuildScoutQuest() : base() { }

    public override int CheckSuccess(TokenizedUnitData Unit)
    {
        return Unit.Type == UnitData.UnitType.Scout ? 1 : 0;
    }

    public override List<Action<TokenizedUnitData>> GetDelegates()
    {
        return Units._OnUnitCreated;
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

    public override IQuestRegister<TokenizedUnitData> GetRegistrar()
    {
        return Game.GetService<Units>();
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

    }

    public override bool ShouldUnlock()
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
