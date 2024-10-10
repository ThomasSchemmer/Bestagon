using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardDeck : CardCollection
{

    protected override void StartServiceInternal()
    {
        base.StartServiceInternal();

        UpdateText();

        Game.RunAfterServicesInit((SaveGameManager Manager, CardFactory Factory) =>
        {
            // needs to have updated costs
            Game.RunAfterServiceInit((RelicService RelicService) =>
            {
                if (Manager.HasDataFor(ISaveableService.SaveGameType.CardDeck))
                    return;

                Cards = new();
                Factory.CreateCard(UnitEntity.UType.Scout, 0, transform, AddScoutCard);
                Factory.CreateCard(BuildingConfig.Type.Woodcutter, 0, transform, AddCard);
                Factory.CreateCard(BuildingConfig.Type.ForagersHut, 0, transform, AddCard);
                Factory.CreateCard(BuildingConfig.Type.Claypit, 0, transform, AddCard);
                Factory.CreateCard(BuildingConfig.Type.Hut, 0, transform, AddCard);
                _OnInit?.Invoke(this);
            });
        });
    }

    private void UpdateText()
    {
        Text.text = "" + Cards.Count;
    }

    public override void OnLoaded()
    {
        base.OnLoaded();
        foreach (Card Card in Cards)
        {
            Card.Show(Card.Visibility.Hidden);
        }

        UpdateText();
        _OnInit?.Invoke(this);
    }


    public override void AddCard(Card Card)
    {
        base.AddCard(Card);
        Card.Show(Card.Visibility.Hidden);
        UpdateText();
    }

    private void AddScoutCard(Card Card)
    {
        EventCard ECard = Card as EventCard;
        GrantUnitEventData EData = ECard.EventData as GrantUnitEventData;
        EData.bIsTemporary = false;
        AddCard(Card);
    }

    public override bool ShouldCardsBeDisplayed()
    {
        return false;
    }

    public override float GetCardSize()
    {
        return 0;
    }
}
