using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using System.Linq;
using System;

/** 
 * Container for a group of cards
 * Used to switch inbetween multiple groups while playing, allowing the player better 
 * access to the multitude of cards available
 * Contains an invisible List for each different @CardState, each containing the bare minimum
 * @CardDTO instead of the GameObject
 * Visually represented by @CardGroupScreens
 */
public class CardGroup
{
    [SaveableList]
    public List<CardDTO> CardDeck;
    [SaveableList]
    public List<CardDTO> CardHand;
    [SaveableList]
    public List<CardDTO> CardStash;
    [SaveableList]
    public List<CardDTO> DiscardDeck;

    [SaveableBaseType]
    public int GroupIndex;
    [SaveableBaseType]
    private string GroupName;

    public CardGroup(int Index)
    {
        CardDeck = new();
        CardHand = new();
        CardStash = new();
        DiscardDeck = new();

        this.GroupIndex = Index;
        SetName(BaseName + Index);
    }

    // necessary for reflection loading
    public CardGroup() { }

    public void InsertCards()
    {
        MoveCardsFromTo(CardDeck, GetCollection<CardDeck>());
        MoveCardsFromTo(CardHand, GetCollection<CardHand>());
        MoveCardsFromTo(CardStash, GetCollection<CardStash>());
        MoveCardsFromTo(DiscardDeck, GetCollection<DiscardDeck>());

        if (Game.IsIn(Game.GameState.CardSelection))
            return;

        if (!Game.TryGetService(out CardHand Hand))
            return;

        if (Hand.Cards.Count > 0)
            return;

        Hand.HandleDelayedFilling();
    }

    public void RemoveCards()
    {
        MoveCardsFromTo(GetCollection<CardDeck>(), CardDeck);
        MoveCardsFromTo(GetCollection<CardHand>(), CardHand);
        MoveCardsFromTo(GetCollection<CardStash>(), CardStash);
        MoveCardsFromTo(GetCollection<DiscardDeck>(), DiscardDeck);
    }

    public CardCollection GetCollection<T>() where T : CardCollection
    {
        if (!Game.IsIn(Game.GameState.CardSelection))
            return Game.GetService<T>();

        // in the CardSelectionScreen, every card should be in the CardDeck 
        // as each CardGroup only has one collection to stay in
        if (typeof(T) != typeof(CardDeck))
            return null;

        if (!Game.TryGetService(out CardGroupManager Manager))
            return null;

        return Manager.CardContainer;
    }

    private void MoveCardsFromTo(CardCollection SourceCollection, List<CardDTO> DTOs)
    {
        if (SourceCollection == null)
            return;

        // don't delete, cause cards might have been stored
        int TotalCount = SourceCollection.Cards.Count;
        for (int i = 0; i < TotalCount; i++)
        {
            Card Card = SourceCollection.RemoveCard();
            CardDTO DTO = CardDTO.CreateFromCard(Card);
            GameObject.DestroyImmediate(Card.gameObject);
            DTOs.Add(DTO);
        }
        SourceCollection.DeleteAllCardsConditionally((Card Card) => { return true; });
    }

    private void MoveCardsFromTo(List<CardDTO> DTOs, CardCollection TargetCollection) {
        if (TargetCollection == null)
            return;

        if (!Game.TryGetService(out CardFactory CardFactory))
            return;

        TargetCollection.DeleteAllCardsConditionally((Card Card) => { return true; });
        foreach (var DTO in DTOs)
        {
            CardFactory.CreateCardFromDTO(DTO, 0, null, TargetCollection.AddCard);
        }
        DTOs.Clear();
    }

    public void StoreCard(Card Card, int CardIndex) {
        if (!Game.IsIn(Game.GameState.CardSelection))
            throw new System.Exception("Should only add single card in card selection");

        if (!Game.TryGetService(out CardGroupManager CardManager))
            return;

        bool bIsActive = CardManager.GetActiveIndex() == GroupIndex;

        if (bIsActive)
        {
            RemoveCards();
        }
        CardDeck.Insert(CardIndex, CardDTO.CreateFromCard(Card));
        GameObject.Destroy(Card.gameObject);
        _OnCardAdded.ForEach(_ => _?.Invoke(GetCardCount()));

        if (bIsActive)
        {
            InsertCards();
        }
    }

    public void InvokeCardRemoved()
    {
        _OnCardRemoved.ForEach(_ => _?.Invoke(GetCardCount()));
    }

    public int GetCardCount()
    {
        if (!Game.TryGetService(out CardGroupManager Manager))
            return 0;

        if (Manager.GetActiveIndex() == GroupIndex)
            return GetActiveCardCount(Manager);

        return CardDeck.Count + CardHand.Count + CardStash.Count + DiscardDeck.Count;
    }

    private int GetActiveCardCount(CardGroupManager Manager)
    {
        if (Game.IsIn(Game.GameState.CardSelection))
            return Manager.GetDisplayedCardCount();

        int Count = 0;
        Count += GetCollection<CardDeck>().Cards.Count;
        Count += GetCollection<CardHand>().Cards.Count;
        Count += GetCollection<CardStash>().Cards.Count;
        Count += GetCollection<DiscardDeck>().Cards.Count;
        return Count;
    }

    public void SetName(string NewName)
    {
        if (NewName.Length > MAX_NAME_LENGTH)
        {
            NewName = NewName[..MAX_NAME_LENGTH];
        }
        for (int i = NewName.Length; i < MAX_NAME_LENGTH; i++)
        {
            NewName = Divider + NewName;
        }
        GroupName = NewName;
    }

    public string GetName()
    {
        return GroupName.Replace(Divider, "");
    }

    public void CleanUpCards()
    {
        CardDeck.AddRange(DiscardDeck);
        DiscardDeck.Clear();
        CardDeck.AddRange(CardHand);
        CardHand.Clear();

        RefreshAllUsages(ref CardDeck);
        RefreshAllUsages(ref CardStash);
        DeleteAllCardsConditionally((Card) =>
        {
            return Card.ShouldBeDeleted();
        }, ref CardStash);
        MoveAllCardsConditionallyTo(ref CardStash, ref CardDeck, (Card) =>
        {
            return Card.bWasUsedUp;
        });
        RefreshAllUsedUps(ref CardDeck);
    }

    private void RefreshAllUsages(ref List<CardDTO> Cards)
    {
        foreach (var Card in Cards)
        {
            if (Card is not BuildingCardDTO)
                continue;

            (Card as BuildingCardDTO).BuildingData.RefreshUsage();
        }
    }

    private void RefreshAllUsedUps(ref List<CardDTO> Cards)
    {
        foreach (var Card in Cards)
        {
            Card.bWasUsedUp = false;
        }
    }

    private void DeleteAllCardsConditionally(Func<CardDTO, bool> Check, ref List<CardDTO> Cards)
    {
        for (int i = Cards.Count - 1; i >= 0; i--)
        {
            CardDTO Card = Cards[i];
            if (Check(Card))
            {
                Cards.Remove(Card);
            }
        }
    }

    private void MoveAllCardsConditionallyTo(ref List<CardDTO> Source, ref List<CardDTO> Target, Func<CardDTO, bool> Check)
    {
        for (int i = Source.Count - 1; i >= 0; i--)
        {
            CardDTO DTO = Source[i];
            if (Check(DTO))
            {
                Source.Remove(DTO);
                Target.Add(DTO);
            }
        }
    }

    public void ApplyPinnedPosition()
    {
        ApplyPinnedPosition(ref CardDeck);
        ApplyPinnedPosition(ref CardHand);
        ApplyPinnedPosition(ref CardStash);
        ApplyPinnedPosition(ref DiscardDeck);
    }
    
    private void ApplyPinnedPosition(ref List<CardDTO> Cards) { 

        CardDTO[] PinnedCardOrder = new CardDTO[Cards.Count];
        List<CardDTO> NonPinnedCards = new();
        foreach (CardDTO DTO in Cards)
        {
            DTO.PinnedIndex = Mathf.Min(DTO.PinnedIndex, PinnedCardOrder.Length - 1);
            if (DTO.PinnedIndex == -1 || PinnedCardOrder[DTO.PinnedIndex] != null)
            {
                NonPinnedCards.Add(DTO);
                continue;
            }

            PinnedCardOrder[DTO.PinnedIndex] = DTO;
        }

        for (int i = 0; i < PinnedCardOrder.Length; i++)
        {
            if (PinnedCardOrder[i] != null)
                continue;

            CardDTO DTO = NonPinnedCards[0];
            NonPinnedCards.Remove(DTO);
            PinnedCardOrder[i] = DTO;
        }

        Cards = PinnedCardOrder.ToList();
    }

    public ActionList<int> _OnCardAdded = new();
    public ActionList<int> _OnCardRemoved = new();

    public static int MAX_NAME_LENGTH = 15;
    public static string BaseName = "CardGroup ";
    public static string Divider = "_";
}
