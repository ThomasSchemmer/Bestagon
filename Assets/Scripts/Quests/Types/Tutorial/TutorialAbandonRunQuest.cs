using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static TutorialSystem;

/** can just use any registrar, though should never actually trigger!*/
public class TutorialAbandonRunQuest : Quest<int>
{
    public TutorialAbandonRunQuest() : base()
    {
    }

    public override int CheckSuccess(int Target)
    {
        return 1;
    }

    public override string GetDescription()
    {
        return "End the tutorial by abandoning the tribe";
    }

    public override int GetMaxProgress()
    {
        return 1;
    }

    public override bool ShouldAutoComplete()
    {
        return true;
    }

    public override Type GetQuestType()
    {
        return Type.Positive;
    }

    public override Dictionary<IQuestRegister<int>, ActionList<int>> GetRegisterMap()
    {
        if (Game.GetService<Turn>() == null)
            return new();

        return new()
        {
            { Game.GetService<Turn>(), Turn._OnRunAbandoned }
        };
    }

    public override Sprite GetSprite()
    {
        if (!Game.TryGetService(out IconFactory IconFactory))
            return null;

        return IconFactory.GetIconForMisc(IconFactory.MiscellaneousType.Abandon);
    }

    public override int GetStartProgress()
    {
        return 0;
    }

    public override void OnAfterCompletion() {
        HexagonConfig.ResetMapSizeToDefault();
    }

    public override void OnCreated()
    {
        if (!Game.TryGetServices(out TutorialSystem TutorialSystem, out Workers Workers))
            return;
        if (!Game.TryGetServices(out CardFactory CardFactory, out Stockpile Stockpile))
            return;

        TutorialSystem.DisplayTextFor(TutorialType.AbandonRun);

        CardFactory.CreateCard(BuildingConfig.Type.Woodcutter, 0, null, AddCard);
        CardFactory.CreateCard(BuildingConfig.Type.Hut, 0, null, AddCard);
        CardFactory.CreateCard(EventData.EventType.GrantUnit, 0, null, AddCard);

        Stockpile.AddResources(new(Production.Type.Wood, 3));
        Workers.CreateNewWorker();
    }
    private void AddCard(Card Card)
    {
        if (!Game.TryGetService(out CardHand CardHand))
            return;

        if (Card is EventCard)
        {
            EventCard ECard = Card as EventCard;
            if (ECard.EventData is not GrantUnitEventData)
                return;

            GrantUnitEventData EventData = (GrantUnitEventData)ECard.EventData;
            EventData.GrantedUnitType = UnitEntity.UType.Worker;
            Card.GenerateCard();
        }

        CardHand.AddCard(Card);
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

    public override bool ShouldUnlockDirectly()
    {
        return true;
    }

    public override void GrantRewards()
    {
    }
}
