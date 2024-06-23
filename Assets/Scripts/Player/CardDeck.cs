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
            Factory.CreateCard(EventData.EventType.ConvertTile, 0, transform, AddCard);
            Factory.CreateCard(UnitData.UnitType.Scout, 0, transform, AddCard);
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

    private void CheckForScout()
    {
        bool bContainsScout = false;
        foreach (Card Card in Cards) {
            if (Card is not EventCard)
                continue;

            EventCard ECard = (EventCard)Card;
            if (ECard.EventData.Type != EventData.EventType.GrantUnit)
                continue;

            GrantUnitEventData UnitEventData = ECard.EventData as GrantUnitEventData;
            if (UnitEventData.GrantedType != UnitData.UnitType.Scout)
                continue;

            bContainsScout = true;
            break;
        }

        if (bContainsScout)
            return;

        if (!Game.TryGetService(out CardFactory Factory))
            return;

        Factory.CreateCard(UnitData.UnitType.Scout, 0, transform, AddDelayedScout);
    }

    private void AddDelayedScout(Card Card)
    {
        AddCard(Card);
        Card.SetIndex(0);
    }

    public override void Load()
    {
        foreach (Card Card in Cards)
        {
            Card.gameObject.SetActive(false);
        }

        CheckForScout();
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
