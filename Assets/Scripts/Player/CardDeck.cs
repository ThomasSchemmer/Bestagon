﻿using System;
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
    }

    private void UpdateText()
    {
        Text.text = "" + Cards.Count;
    }

    private void CheckForScout()
    {
        if (!Game.TryGetService(out TutorialSystem TutorialSystem) || TutorialSystem.IsInTutorial())
            return;

        bool bContainsScout = false;
        foreach (Card Card in Cards) {
            if (Card is not EventCard)
                continue;

            EventCard ECard = (EventCard)Card;
            if (ECard.EventData.Type != EventData.EventType.GrantUnit)
                continue;

            GrantUnitEventData UnitEventData = ECard.EventData as GrantUnitEventData;
            if (UnitEventData.GrantedType != UnitEntity.UType.Scout)
                continue;

            bContainsScout = true;
            break;
        }

        if (bContainsScout)
            return;

        if (!Game.TryGetService(out CardFactory Factory))
            return;

        Factory.CreateCard(UnitEntity.UType.Scout, 0, transform, AddDelayedScout);
    }

    private void AddDelayedScout(Card Card)
    {
        AddCard(Card);
        SetIndex(Card, 0);
    }

    public override void Load()
    {
        foreach (Card Card in Cards)
        {
            Card.Show(Card.Visibility.Hidden);
        }

        CheckForScout();
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
