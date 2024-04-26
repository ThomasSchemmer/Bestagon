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

        if (!Game.TryGetService(out SaveGameManager Manager))
            return;

        // will be loaded instead of generated
        if (Manager.HasDataFor(ISaveable.SaveGameType.CardHand))
            return;

        _OnInit?.Invoke();
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

        UpdateText();
        _OnInit?.Invoke();
    }


    public override void AddCard(Card Card)
    {
        base.AddCard(Card);
        Card.gameObject.SetActive(false);
        UpdateText();
    }
}
