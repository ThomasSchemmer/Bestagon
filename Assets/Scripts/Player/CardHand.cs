using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardHand : CardCollection
{
    void Start()
    {
        Instance = this;
        Cards = new List<Card> {
            Card.CreateCard<Woodcutter>(0, CardPrefab, transform),
            Card.CreateCard<Mine>(1, CardPrefab, transform),
            Card.CreateCard<Farm>(2, CardPrefab, transform),
            Card.CreateCard<Farm>(3, CardPrefab, transform),
            Card.CreateCard<Woodcutter>(4, CardPrefab, transform)
        };
        Sort(false);
    }

    public void Sort(bool IsACardHovered) {
        transform.localPosition = IsACardHovered ? HoverPosition : NormalPosition;
        int i = 0;
        int Count = Cards.Count;
        foreach(Card Card in Cards) {
            Card.transform.SetSiblingIndex(Count - 1 - Card.GetIndex());
            float offset = i - Cards.Count / 2.0f;
            float SelectOffset = Card.IsSelected() ? Card.SelectOffset : 0;
            Card.transform.localPosition = new Vector3(offset * 75, SelectOffset, 0);
            Card.name = "Card " + i;
            i++;
        }
        Canvas.ForceUpdateCanvases();
    }

    public void DiscardCard(Card Card) {
        RemoveCard(Card);
        int i = 0;
        foreach(Card Other in Cards) {
            Other.SetIndex(i);
            i++;
        }
        Sort(false);

        DiscardDeck.Instance.AddCard(Card);
    }

    public override void AddCard(Card Card) {
        base.AddCard(Card);
        Card.gameObject.SetActive(true);
        Card.gameObject.layer = LayerMask.NameToLayer("Card");
        int i = 0;
        foreach (Card Other in Cards) {
            Other.SetIndex(i);
            i++;
        }
        Sort(false);
    }

    public static Vector3 NormalPosition = new Vector3(0, -500, 0);
    public static Vector3 HoverPosition = new Vector3(0, -350, 0);

    public static CardHand Instance;

}
