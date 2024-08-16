using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static TutorialSystem;

public class TutorialCardsQuest : Quest<Card>
{
    public TutorialCardsQuest() : base()
    {
    }

    public override int CheckSuccess(Card Target)
    {
        return 1;
    }

    public override string GetDescription()
    {
        return "Select the treasure card and play it on a tile";
    }

    public override int GetMaxProgress()
    {
        return 1;
    }

    public override Type GetQuestType()
    {
        return Type.Positive;
    }

    public override Dictionary<IQuestRegister<Card>, ActionList<Card>> GetRegisterMap()
    {
        if (Game.GetService<CardHand>() == null)
            return new();

        return new()
        {
            { Game.GetService<CardHand>(), CardHand._OnCardPlayed }
        };
    }

    public override Sprite GetSprite()
    {
        if (!Game.TryGetService(out IconFactory IconFactory))
            return null;

        return IconFactory.GetIconForMisc(IconFactory.MiscellaneousType.Tile);
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

        CardFactory.CreateCard(EventData.EventType.GrantResource, 0, null, AddCard);

        TutorialSystem.DisplayTextFor(TutorialType.Cards);
    }

    private void AddCard(Card Card)
    {
        if (!Game.TryGetService(out CardHand CardHand))
            return;

        if (Card is not EventCard)
            return;

        EventCard ECard = Card as EventCard;
        if (ECard.EventData is not GrantResourceEventData)
            return;

        GrantResourceEventData EventData = (GrantResourceEventData) ECard.EventData;
        EventData.GrantedResource = new(Production.Type.Wood, 5);
        Card.GenerateCard();

        CardHand.AddCard(Card);
    }

    public override bool AreRequirementsFulfilled()
    {
        return true;
    }

    public override bool TryGetNextType(out System.Type Type)
    {
        Type = typeof(TutorialBuildingsQuest);
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
