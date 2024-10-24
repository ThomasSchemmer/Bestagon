using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Holds all disabled cards that the player cannot play anymore
 * This could be temporary used-up cards, or generally disabled cards
 */
public class CardStash : CardCollection
{
    protected override void StartServiceInternal()
    {
        base.StartServiceInternal();
        _OnInit?.Invoke(this);
    }

    public override void OnLoaded()
    {
        base.OnLoaded();
        foreach (Card Card in Cards)
        {
            Card.gameObject.SetActive(false);
        }
    }

    public override void AddCard(Card Card)
    {
        base.AddCard(Card);
        Card.Show(Card.Visibility.Hidden);
    }

    public override bool ShouldCardsBeDisplayed()
    {
        return false;
    }

    public override float GetCardSize()
    {
        return 1;
    }

    public override Card.CardState GetState()
    {
        return Card.CardState.Disabled;
    }
    public override bool ShouldUpdateCardState()
    {
        return true;
    }
}
