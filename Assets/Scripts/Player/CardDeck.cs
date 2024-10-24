using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Holds all cards that will be available to the player to draw from every turn
 * Once empty, will be refilled with shuffled cards from the @DiscardDeck
 */
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

    public override bool ShouldCardsBeDisplayed()
    {
        return false;
    }

    public override float GetCardSize()
    {
        return 0;
    }

    public override Card.CardState GetState()
    {
        return Card.CardState.Available;
    }
    public override bool ShouldUpdateCardState()
    {
        return true;
    }
}
