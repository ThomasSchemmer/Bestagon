using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Turn : MonoBehaviour
{
    public void NextTurn() {
        MessageSystem.DeleteAllMessages();
        Stockpile.ProduceResources();
        TurnNr++;
        MoveCard();
        SpreadMalaise();
        UpdateMalaiseVisualization();
        Workers.HandleEndOfTurn();
        UpdateSelection();
        MiniMap.Instance.FillBuffer();
    }

    private void SpreadMalaise() {
        int Count = ActiveMalaises.Count;
        for (int i = 0; i < Count; i++) {
            MalaiseData Data = ActiveMalaises[i];
            Data.Spread();
        }
    }

    private void UpdateMalaiseVisualization() {
        foreach (MalaiseData Data in ActiveMalaises) {
            if (!Data.Chunk.Visualization)
                continue;

            Data.Chunk.Visualization.MalaiseVisualization.GenerateMesh();
        }
    }

    private void MoveCard() {
        Card RemovedCard = CardDeck.RemoveCard(); 
        if (RemovedCard == null) 
            return; 

        CardHand.AddCard(RemovedCard);


        //access deck, select first card from deck
        // acess hand, Add it to hand - remove it from the deck

    }

    private void UpdateSelection() {
        // this triggers all visualizations for the selected hex
        HexagonVisualization Hex = Selector.GetSelectedHexagon();
        if (Hex == null) 
            return;

        Selector.SelectHexagon(Hex);
    }

    public static int TurnNr = 1;
    public static List<MalaiseData> ActiveMalaises = new List<MalaiseData>();
}
