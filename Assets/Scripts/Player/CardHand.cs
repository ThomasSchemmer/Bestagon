using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public class CardHand : CardCollection
{

    protected override void StartServiceInternal()
    {
        base.StartServiceInternal();

        if (!Game.TryGetService(out SaveGameManager Manager))
            return;

        // will be loaded
        if (Manager.HasDataFor(ISaveable.SaveGameType.CardHand))
            return;

        Cards = new List<Card> {
            Card.CreateCard(BuildingData.Type.Woodcutter, 0, transform),
            //Card.CreateCard(BuildingData.Type.Mine, 1, transform),
            //Card.CreateCard(BuildingData.Type.Farm, 2, transform),
            //Card.CreateCard(BuildingData.Type.Farm, 3, transform),
            //Card.CreateCard(BuildingData.Type.Woodcutter, 4, transform)
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
        if (!Game.TryGetService(out DiscardDeck Deck))
            return;

        DiscardCard(Card, Deck);
    }

    public void DiscardCard(Card Card, CardCollection Target)
    {
        RemoveCard(Card);
        int i = 0;
        foreach (Card Other in Cards)
        {
            Other.SetIndex(i);
            i++;
        }
        Sort(false);

        Target.AddCard(Card);
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

    public override void Load()
    {
        
        Sort(false);
    }

    public static Vector3 NormalPosition = new Vector3(0, -500, 0);
    public static Vector3 HoverPosition = new Vector3(0, -350, 0);
}
