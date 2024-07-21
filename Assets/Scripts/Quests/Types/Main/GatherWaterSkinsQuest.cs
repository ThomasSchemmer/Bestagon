using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GatherWaterSkinsQuest : Quest<Production>
{

    public GatherWaterSkinsQuest() : base() { }

    public override int CheckSuccess(Production Production)
    {
        int Count = 0;
        foreach (var Item in Production.GetTuples())
        {
            if (Item.Key != Production.Type.WaterSkins)
                continue;

            Count += Item.Value;
        }
        return Count;
    }

    public override List<Action<Production>> GetDelegates()
    {
        return Stockpile._OnResourcesCollected;
    }

    public override string GetDescription()
    {
        return "Build the well and produce additional water skins";
    }

    public override int GetMaxProgress()
    {
        return 5;
    }

    public override Type GetQuestType()
    {
        return Type.Main;
    }

    public override IQuestRegister<Production> GetRegistrar()
    {
        return Game.GetService<Stockpile>();
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

    }

    public override bool ShouldUnlock()
    {
        return true;
    }

    public override bool TryGetNextType(out System.Type Type)
    {
        Type = default;
        return false;
    }

    public override void GrantRewards()
    {
        GrantResources(new Production(Production.Type.Clay, 3));
    }
}
