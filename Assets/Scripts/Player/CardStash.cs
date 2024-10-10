using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardStash : CardCollection
{
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
}
