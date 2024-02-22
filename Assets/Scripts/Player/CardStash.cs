using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardStash : CardCollection
{
    public override void Load()
    {
        foreach (Card Card in Cards)
        {
            Card.gameObject.SetActive(false);
        }
    }

    public override void AddCard(Card Card)
    {
        base.AddCard(Card);
        Card.gameObject.SetActive(false);
    }
}
