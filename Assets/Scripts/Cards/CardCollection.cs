using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

/**
 * Baseclass for any collection of cards
 * Allows for adding and removing and other utility functions
 */
public abstract class CardCollection : GameService
{
    public virtual void AddCard(Card Card) {
        Cards.Add(Card);
        Card.transform.SetParent(transform, false);
        Card.SetCanBeHovered(false);
        Card.OnAssignedToCollection(this);
    }

    public virtual Card RemoveCard() {
        if (Cards.Count == 0)
            return null;

        Card CardToRemove = Cards[0];
        return RemoveCard(CardToRemove);
    }

    public virtual Card RemoveCard(Card Card)
    {
        Cards.Remove(Card);

        if (!Game.IsIn(Game.GameState.CardSelection))
        {
            Card.Animations.Add(new()
            {
                StartPosition = transform.position,
                SourceCollection = this
            });
        }

        if (Text)
        {
            Text.text = "" + Cards.Count;
        }
        return Card;
    }

    public void MoveAllCardsTo(CardCollection OtherCollection)
    {
        MoveCardsTo(Cards, OtherCollection);
    }

    public void MoveCardsTo(List<Card> CardsToMove, CardCollection OtherCollection)
    {
        while (CardsToMove.Count > 0)
        {
            Card Card = CardsToMove[0];
            RemoveCard(Card);
            OtherCollection.AddCard(Card);
            CardsToMove.Remove(Card);
        }
    }

    public void MoveAllCardsConditionallyTo(CardCollection OtherCollection, Func<Card, bool> Check)
    {
        for (int i = Cards.Count - 1; i >= 0; i--)
        {
            Card Card = Cards[i];
            if (Check(Card))
            {
                RemoveCard(Card);
                OtherCollection.AddCard(Card);
            }
        }
    }

    public void SetIndex(Card Card, int Index)
    {
        if (Cards.Contains(Card))
        {
            Cards.Remove(Card);
        }

        Cards.Insert(Index, Card);
        Card.transform.SetSiblingIndex(Index);
    }

    public void UpdatePinnedIndices()
    {
        foreach (Card Card in Cards)
        {
            Card.SetPinned(Card.IsPinned() ? Card.transform.GetSiblingIndex() : -1);
        }
    }

    public void DeleteAllCardsConditionally(Func<Card, bool> Check)
    {
        for (int i = Cards.Count - 1; i >= 0; i--)
        {
            Card Card = Cards[i];
            if (Check(Card))
            {
                RemoveCard(Card);
                DestroyImmediate(Card.gameObject);
            }
        }
    }

    public virtual void Sort() { }

    protected override void StartServiceInternal()
    {
        Show(true);
    }

    protected override void StopServiceInternal()
    {
        Show(false);
    }

    public void RefreshAllUsages()
    {
        foreach (Card Card in Cards)
        {
            if (Card is not BuildingCard)
                continue;

            (Card as BuildingCard).RefreshUsage();
        }
    }

    public void RefreshAllUsedUps()
    {
        foreach (Card Card in Cards)
        {
            Card.RefreshUsedUp();
        }
    }

    public void Show(bool bShow)
    {
        gameObject.SetActive(bShow);
    }

    public abstract bool ShouldUpdateCardState();
    public abstract Card.CardState GetState();
    public abstract bool ShouldCardsBeDisplayed();
    public abstract float GetCardSize();

    // only temporarily holds cards while playing! 
    public List<Card> Cards = new List<Card>();
    public TMPro.TextMeshProUGUI Text;

    protected override void ResetInternal()
    {
        Cards = new();
    }
}
