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

        Game.RunAfterServiceInit((CardFactory Factory) =>
        {
            Cards = new();
            Factory.CreateCard(UnitData.UnitType.Scout, 0, transform, AddCard);
            Factory.CreateCard(EventData.EventType.GrantUnit, 0, transform, AddCard);
            Factory.CreateCard(BuildingConfig.Type.Woodcutter, 0, transform, AddCard);
            Factory.CreateCard(BuildingConfig.Type.ForagersHut, 0, transform, AddCard);
            Factory.CreateCard(BuildingConfig.Type.Claypit, 0, transform, AddCard);
            Factory.CreateCard(BuildingConfig.Type.Hut, 0, transform, AddCard);
            _OnInit?.Invoke();
        });

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
