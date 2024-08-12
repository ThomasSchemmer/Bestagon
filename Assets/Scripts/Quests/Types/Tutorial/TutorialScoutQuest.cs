using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static TutorialSystem;

public class TutorialScoutQuest : Quest<TokenizedUnitData>
{
    public TutorialScoutQuest() : base()
    {
    }

    public override int CheckSuccess(TokenizedUnitData Target)
    {
        return 1;
    }

    public override string GetDescription()
    {
        return "Build and staff a foragers hut, recruit and move the scout";
    }

    public override int GetMaxProgress()
    {
        return 1;
    }

    public override Type GetQuestType()
    {
        return Type.Positive;
    }

    public override IQuestRegister<TokenizedUnitData> GetRegistrar()
    {
        return Game.GetService<Units>();
    }

    public override ActionList<TokenizedUnitData> GetDelegates()
    {
        return Units._OnUnitMoved;
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

    public override void OnAfterCompletion() { }

    public override void OnCreated()
    {
        if (!Game.TryGetService(out TutorialSystem TutorialSystem))
            return;
        if (!Game.TryGetService(out CardFactory CardFactory))
            return;

        CardFactory.CreateCard(BuildingConfig.Type.ForagersHut, 0, null, AddCard);
        CardFactory.CreateCard(UnitData.UnitType.Scout, 0, null, AddCard);
        TutorialSystem.DisplayTextFor(TutorialType.Scouts);
    }

    private void AddCard(Card Card)
    {
        if (!Game.TryGetService(out CardHand CardHand))
            return;

        CardHand.AddCard(Card);
    }

    public override bool AreRequirementsFulfilled()
    {
        return true;
    }

    public override bool TryGetNextType(out System.Type Type)
    {
        Type = typeof(TutorialMalaiseQuest);
        return true;
    }

    public override bool ShouldUnlockDirectly()
    {
        return true;
    }

    public override void GrantRewards()
    {
    }
}
