﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardDeck : CardCollection
{
    protected override void StartServiceInternal()
    {
        base.StartServiceInternal();

        if (!Game.TryGetService(out SaveGameManager Manager))
            return;

        // will be loaded instead of generated
        if (Manager.HasDataFor(ISaveable.SaveGameType.CardHand))
            return;

    }

    public void AddGeneratedCard(Card Card)
    {
        Card.transform.localPosition = new Vector3(0, Card.GetIndex() * 15, 0);
        Card.gameObject.layer = 0;
        Card.gameObject.SetActive(false);
        Cards.Add(Card);
    }

    private void UpdateText()
    {
        Text.text = "" + Cards.Count;
    }

    public override void Load()
    {
        foreach (Card Card in Cards)
        {
            Card.gameObject.SetActive(false);
        }

        if (Game.IsIn(Game.GameState.Game) || Game.IsIn(Game.GameState.GameMenu))
        {
            Fill(2);
        }
        UpdateText();
    }

    private void Fill(int MaxAmount)
    {
        if (!Game.TryGetService(out CardHand Hand))
            return;

        MaxAmount = Mathf.Min(MaxAmount, Cards.Count);

        for (int i = Hand.Cards.Count; i < MaxAmount; i++)
        {
            Card Card = RemoveCard();
            Hand.AddCard(Card);
        }
    }

    public override void AddCard(Card Card)
    {
        base.AddCard(Card);
        Card.gameObject.SetActive(false);
        UpdateText();
    }
}
