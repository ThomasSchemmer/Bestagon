using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardDeck : MonoBehaviour
{
    void Start()
    {
        CardDeck.Instance = this;
        Cards = new List<Card>();
          
        for (int i = 0; i < 10; i++) {
            Card Card = CreateRandomCard(i);
            Card.transform.localPosition = new Vector3(0, Mathf.Min(i, 5) * 15, 0);
            Card.gameObject.layer = 0;
            Cards.Add(Card);
        }
    }

    private Card CreateRandomCard(int i) {
        int TypeCount = Enum.GetNames(typeof(BuildingData.Type)).Length;
        BuildingData.Type Type = (BuildingData.Type)UnityEngine.Random.Range(1, TypeCount);
        switch (Type) {
            case BuildingData.Type.Woodcutter: return Card.CreateCard<Woodcutter>(i, CardPrefab, transform);
            case BuildingData.Type.Farm: return Card.CreateCard<Farm>(i, CardPrefab, transform);
            case BuildingData.Type.Mine: return Card.CreateCard<Mine>(i, CardPrefab, transform);
        }
        return null;
    }

    public static Card RemoveCard() { 
        if (Instance.Cards.Count == 0) 
            return null;
        
        Card RemovedCard = Instance.Cards[0];
        Instance.Cards.RemoveAt(0);
        return RemovedCard;
    }

    public GameObject CardPrefab;
    public List<Card> Cards;
    public static CardDeck Instance;
}
