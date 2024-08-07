﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Turn : GameService, IQuestRegister<int>
{
    public void OnEnable()
    {
        Game.Instance._OnPause += OnPause;
        Game.Instance._OnResume += OnResume;
    }
    protected override void StartServiceInternal()
    {
        CardHand = Game.GetService<CardHand>();
        CardDeck = Game.GetService<CardDeck>();
        DiscardDeck = Game.GetService<DiscardDeck>();
        Stockpile = Game.GetService<Stockpile>();
        MiniMap = Game.GetService<MiniMap>();
        Units = Game.GetService<Units>();
        CloudRenderer = Game.GetService<CloudRenderer>();
        Selectors = Game.GetService<Selectors>();
        Quests = Game.GetService<QuestService>();

        gameObject.SetActive(true);
        gameObject.SetActive(true);
        _OnInit?.Invoke(this);
    }

    protected override void StopServiceInternal() {
        gameObject.SetActive(false);
        gameObject.SetActive(false);
    }

    public void NextTurn() {
        if (!IsEnabled || !IsInit)
            return;

        MessageSystemScreen.DeleteAllMessages();
        Stockpile.GenerateResources();
        Stockpile.ProduceWorkers();
        TurnNr++;
        Units.RefreshUnits();
        MoveCard();
        CloudRenderer.SpreadMalaise();

        UpdateSelection();

        MiniMap.FillBuffer();
        Selectors.DeselectCard();
        Quests.CheckForQuestsToUnlock();

        _OnTurnEnd?.Invoke();
        _OnTurnEnded.ForEach(_ => _.Invoke(TurnNr));
    }


    private void OnPause()
    {
        IsEnabled = false;
    }

    private void OnResume()
    {
        IsEnabled = true;
    }

    private void MoveCard() {
        if (CardDeck.Cards.Count == 0) {
            FillCardDeck();
        }

        // already at maximum amount, so just wait 
        if (CardHand.Cards.Count >= CardHand.AMOUNT_HANDCARDS_MAX)
            return;

        Card RemovedCard = CardDeck.RemoveCard();
        if (CardHand.Cards.Count == 0 && RemovedCard == null)
        {
            Game.Instance.GameOver("You have run out of cards to play!");
        }
        if (RemovedCard == null)
            return;

        CardHand.AddCard(RemovedCard);
    }

    private void FillCardDeck() {
        List<Card> Cards = new();
        // first remove every card
        Card CurrentCard = DiscardDeck.RemoveCard();
        while (CurrentCard != null) {
            Cards.Add(CurrentCard);
            CurrentCard = DiscardDeck.RemoveCard();
        }

        // then shuffle
        for (int i = 0; i < Cards.Count; i++) {
            int TargetIndex = Random.Range(i, Cards.Count);
            Card Temp = Cards[i];
            Cards[i] = Cards[TargetIndex];
            Cards[TargetIndex] = Temp;  
        }

        // and then add into the card deck
        for (int i = 0; i < Cards.Count; i++) {
            CardDeck.AddCard(Cards[i]);
        }
    }

    private void UpdateSelection() {
        if (!Game.TryGetService(out Selectors Selector))
            return;

        // this triggers all visualizations for the selected hex
        HexagonVisualization Hex = Selector.GetSelectedHexagon();
        if (Hex == null) 
            return;

        Selector.SelectHexagon(Hex);
    }

    public void Show(bool bShow)
    {
        gameObject.SetActive(bShow);
        if (AbandonScreen != null)
        {
            AbandonScreen.Show(bShow);
        }
    }

    private bool IsEnabled = true;
    private CardHand CardHand;
    private CardDeck CardDeck;
    private DiscardDeck DiscardDeck;
    private Stockpile Stockpile;
    private MiniMap MiniMap;
    private Units Units;
    private CloudRenderer CloudRenderer;
    private Selectors Selectors;
    private QuestService Quests;

    public int TurnNr = 0;

    public AbandonScreen AbandonScreen;

    public delegate void OnTurnEnd();
    public static OnTurnEnd _OnTurnEnd;

    public static ActionList<int> _OnTurnEnded = new();
}
