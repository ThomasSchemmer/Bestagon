using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Turn : GameService
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
        gameObject.SetActive(true);
        TurnUI.SetActive(true);
        IsInit = true;
        _OnInit?.Invoke();
    }

    protected override void StopServiceInternal() {
        gameObject.SetActive(false);
        TurnUI.SetActive(false);
    }

    public void NextTurn() {
        if (!IsEnabled || !IsInit)
            return;

        MessageSystem.DeleteAllMessages();
        Stockpile.ProduceResources();
        TurnNr++;
        MoveCard();
        SpreadMalaise();
        UpdateMalaiseVisualization();

        if (!Game.TryGetService(out Workers WorkerService))
            return;
        WorkerService.HandleEndOfTurn();

        UpdateSelection();
        if (!Game.TryGetService(out MiniMap Map))
            return;

        Map.FillBuffer();
    }

    private void SpreadMalaise() {
        int Count = ActiveMalaises.Count;
        for (int i = 0; i < Count; i++) {
            MalaiseData Data = ActiveMalaises[i];
            Data.Spread();
        }
    }

    private void OnPause()
    {
        IsEnabled = false;
    }

    private void OnResume()
    {
        IsEnabled = true;
    }

    private void UpdateMalaiseVisualization() {
        foreach (MalaiseData Data in ActiveMalaises) {
            if (!Data.Chunk.Visualization)
                continue;

            Data.Chunk.Visualization.MalaiseVisualization.GenerateMesh();
        }
    }

    private void MoveCard() {
        if (CardDeck.Cards.Count == 0) {
            FillCardDeck();
        }

        Card RemovedCard = CardDeck.RemoveCard(); 
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
        if (!Game.TryGetService(out Selector Selector))
            return;

        // this triggers all visualizations for the selected hex
        HexagonVisualization Hex = Selector.GetSelectedHexagon();
        if (Hex == null) 
            return;

        Selector.SelectHexagon(Hex);
    }

    private bool IsEnabled = true;
    private CardHand CardHand;
    private CardDeck CardDeck;
    private DiscardDeck DiscardDeck;

    public int TurnNr = 1;
    public List<MalaiseData> ActiveMalaises = new List<MalaiseData>();
    public GameObject TurnUI;
}
