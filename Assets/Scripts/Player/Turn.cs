using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Turn : MonoBehaviour
{
    public void OnEnable()
    {
        Instance = this;
        Game.Instance._OnPause += OnPause;
        Game.Instance._OnResume += OnResume;
    }

    public void NextTurn() {
        if (!IsEnabled)
            return;

        MessageSystem.DeleteAllMessages();
        Stockpile.ProduceResources();
        TurnNr++;
        MoveCard();
        SpreadMalaise();
        UpdateMalaiseVisualization();
        Workers.HandleEndOfTurn();
        UpdateSelection();
        MiniMap Map = Game.GetService<MiniMap>();
        if (!Map)
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
        if (CardDeck.Instance.Cards.Count == 0) {
            FillCardDeck();
        }

        Card RemovedCard = CardDeck.Instance.RemoveCard(); 
        if (RemovedCard == null) 
            return; 

        CardHand.Instance.AddCard(RemovedCard);
    }

    private void FillCardDeck() {
        List<Card> Cards = new();
        // first remove every card
        Card CurrentCard = DiscardDeck.Instance.RemoveCard();
        while (CurrentCard != null) {
            Cards.Add(CurrentCard);
            CurrentCard = DiscardDeck.Instance.RemoveCard();
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
            CardDeck.Instance.AddCard(Cards[i]);
        }
    }

    private void UpdateSelection() {
        // this triggers all visualizations for the selected hex
        HexagonVisualization Hex = Selector.GetSelectedHexagon();
        if (Hex == null) 
            return;

        Selector.SelectHexagon(Hex);
    }

    private bool IsEnabled = true;

    public int TurnNr = 1;
    public List<MalaiseData> ActiveMalaises = new List<MalaiseData>();

    public static Turn Instance;
}
