using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MalaisedSpawnQuest : Quest<TokenizedUnitData>
{
    public MalaisedSpawnQuest() : base() { }

    public override int CheckSuccess(TokenizedUnitData Unit)
    {
        return 1;
    }

    public override List<Action<TokenizedUnitData>> GetDelegates()
    {
        return Units._OnUnitMoved;
    }

    public override string GetDescription()
    {
        return "Reach the target location before the timer runs out!";
    }

    public override int GetMaxProgress()
    {
        return 3;
    }

    public override Type GetQuestType()
    {
        return Type.Negative;
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

    public override void OnAfterCompletion(){}

    public override bool ShouldUnlock()
    {
        return true;
    }

    public override bool TryGetNextType(out System.Type Type)
    {
        Type = default;
        return false;
    }
    public override void GrantRewards() { }

    public static Location TargetLocation;
}
