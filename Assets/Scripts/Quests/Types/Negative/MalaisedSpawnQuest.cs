using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MalaisedSpawnQuest : MultiQuest<int, TokenizedUnitData>
{
    public override System.Type GetCancelQuestType()
    {
        return typeof(MalaisedSpawnQuestMove);
    }

    public override System.Type GetMultiQuestType()
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

    public override ActionList<int> GetDelegates()
    {
        return Turn._OnTurnEnded;
    }
    public override void OnCreated() { }

    public override string GetDescription()
    {
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

    public override IQuestRegister<int> GetRegistrar()
    {
        return Game.GetService<Turn>();
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

    public override void OnAfterCompletion() {
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
public class MalaisedSpawnQuestMove : Quest<TokenizedUnitData>
{
    public MalaisedSpawnQuestMove() : base() { }

    public override int CheckSuccess(TokenizedUnitData Unit)
    {
        return Unit.Location.Equals(MalaisedSpawnQuest.TargetLocation) ? 1 : 0;
    }

    public override ActionList<TokenizedUnitData> GetDelegates()
    {
        return Units._OnUnitMoved;
    }
    public override void OnCreated() { }

    public override string GetDescription()
    {
        return "Reach "+ MalaisedSpawnQuest.TargetLocation.GlobalTileLocation.ToString()+" before the timer runs out!";
    }

    public override int GetMaxProgress()
    {
        return 1;
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
