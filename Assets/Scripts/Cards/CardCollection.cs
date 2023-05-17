using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class CardCollection : MonoBehaviour
{
    public virtual void AddCard(Card Card) {
        Cards.Add(Card);
        Card.transform.SetParent(transform, false);
    }

    public virtual Card RemoveCard() {
        if (Cards.Count == 0)
            return null;

        Card RemovedCard = Cards[0];
        Cards.RemoveAt(0);
        if (Text) {
            Text.text = "" + Cards.Count;
        }
        return RemovedCard;
    }

    public virtual void RemoveCard(Card Card) {
        Cards.Remove(Card);
        if (Text) {
            Text.text = "" + Cards.Count;
        }
    }

    protected Card CreateRandomCard(int i) {
        int TypeCount = Enum.GetNames(typeof(BuildingData.Type)).Length;
        BuildingData.Type Type = (BuildingData.Type)UnityEngine.Random.Range(1, TypeCount);
        switch (Type) {
            case BuildingData.Type.Woodcutter: return Card.CreateCard<Woodcutter>(i, CardPrefab, transform);
            case BuildingData.Type.Farm: return Card.CreateCard<Farm>(i, CardPrefab, transform);
            case BuildingData.Type.Mine: return Card.CreateCard<Mine>(i, CardPrefab, transform);
        }
        return null;
    }


    public List<Card> Cards = new List<Card>();
    public TMPro.TextMeshProUGUI Text;
    public GameObject CardPrefab;
}
