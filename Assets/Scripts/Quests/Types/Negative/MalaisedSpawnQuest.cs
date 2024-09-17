using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MalaisedSpawnQuest : MultiQuest<int, TokenizedUnitEntity>
{
    public override System.Type GetQuest2Type()
    {
        return typeof(MalaisedSpawnQuestMove);
    }

    public override System.Type GetQuest1Type()
    {
        return typeof(MalaisedSpawnQuestTurn);
    }

    public override bool AreRequirementsFulfilled()
    {
        return true;
    }

    public override bool TryGetNextType(out System.Type Type)
    {
        Type = default;
        return false;
    }
    public override void OnCreated() { }

    public override bool IsQuest2Cancel()
    {
        return true;
    }

    public static Location TargetLocation;
    public static int StartTurnNr;
}

public class MalaisedSpawnQuestTurn : Quest<int>
{
    public MalaisedSpawnQuestTurn() : base() { }

    public override int CheckSuccess(int Turn)
    {
        return 1;
    }

    public override void OnCreated() { }

    public override string GetDescription()
    {
        if (MalaisedSpawnQuest.TargetLocation == null)
            return "";

        return "Reach " + MalaisedSpawnQuest.TargetLocation.GlobalTileLocation.ToString() + " before the timer runs out!";
    }

    public override int GetMaxProgress()
    {
        return 7;
    }

    public override Type GetQuestType()
    {
        return Type.Negative;
    }

    public override Dictionary<IQuestRegister<int>, ActionList<int>> GetRegisterMap()
    {
        if (Game.GetService<Turn>() == null)
            return new();

        return new()
        {
            { Game.GetService<Turn>(), Turn._OnTurnEnded }
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
        if (MalaisedSpawnQuest.TargetLocation == null)
            return;
        MessageSystemScreen.CreateMessage(Message.Type.Error, "Your scouts were not able to stop the spread of the malaise!");
        MalaiseData.SpreadInitially(MalaisedSpawnQuest.TargetLocation);
    }

    public override bool AreRequirementsFulfilled()
    {
        return true;
    }

    public override bool TryGetNextType(out System.Type Type)
    {
        Type = default;
        return false;
    }

    public override void GrantRewards() { }

}
public class MalaisedSpawnQuestMove : Quest<TokenizedUnitEntity>
{
    public MalaisedSpawnQuestMove() : base() { }

    public override int CheckSuccess(TokenizedUnitEntity Unit)
    {
        return Unit.GetLocation().Equals(MalaisedSpawnQuest.TargetLocation) ? 1 : 0;
    }

    public override void OnCreated() { }

    public override string GetDescription()
    {
        string Location = MalaisedSpawnQuest.TargetLocation != null ? MalaisedSpawnQuest.TargetLocation.GlobalTileLocation.ToString() : string.Empty;
        return "Reach "+ Location + " before the timer runs out!";
    }

    public override int GetMaxProgress()
    {
        return 1;
    }

    public override Type GetQuestType()
    {
        return Type.Negative;
    }

    public override Dictionary<IQuestRegister<TokenizedUnitEntity>, ActionList<TokenizedUnitEntity>> GetRegisterMap()
    {
        if (Game.GetService<Units>() == null)
            return new();

        return new()
        {
            { Game.GetService<Units>(), Units._OnUnitMoved }
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

    public override void OnAfterCompletion(){
        MessageSystemScreen.CreateMessage(Message.Type.Success, "Your scouts were able to contain the building malaise");
    }

    public override bool AreRequirementsFulfilled()
    {
        return true;
    }

    public override bool TryGetNextType(out System.Type Type)
    {
        Type = default;
        return false;
    }

    public override void GrantRewards() { }
}
