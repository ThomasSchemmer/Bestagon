using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardDeck : CardCollection
{
    protected override void StartServiceInternal()
    {
        base.StartServiceInternal();
        Cards = new List<Card>();

        for (int i = 0; i < 10; i++)
        {
            Card Card = CreateRandomCard(i);
            Card.transform.localPosition = new Vector3(0, Mathf.Min(i, 5) * 15, 0);
            Card.gameObject.layer = 0;
            Card.gameObject.SetActive(false);
            Cards.Add(Card);
        }
        Text.text = "" + Cards.Count;
    }
}
