using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public abstract class CardCollection : GameService, ISaveableService
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
        Card.SetIndex(Index);
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

    protected override void StopServiceInternal() {
        Show(false);
    }

    public int GetSize()
    {
        //byte count and amount of cards
        int Size = sizeof(int) * 2;
        Size += GetCardsSize();
        return Size;
    }

    private int GetCardsSize()
    {
        int Size = 0;
        foreach (Card Card in Cards)
        {
            Size += GetCardSize(Card);
        }

        return Size;
    }

    private int GetCardSize(Card Card)
    {
        // all building data has the same size
        if (Card is BuildingCard)
            return BuildingCardDTO.GetStaticSize();

        // but event data size is dependent on its type!
        if (Card is EventCard)
            return EventCardDTO.GetStaticSize(((EventCard)Card).EventData.Type);

        return CardDTO.GetStaticSize();
    }

    public byte[] GetData()
    {
        NativeArray<byte> Bytes = new(GetSize(), Allocator.Temp);
        int Pos = 0;

        Pos = SaveGameManager.AddInt(Bytes, Pos, GetSize());
        Pos = SaveGameManager.AddInt(Bytes, Pos, Cards.Count);

        foreach (Card Card in Cards)
        {
            CardDTO DTO = CardDTO.CreateFromCard(Card);
            Pos = SaveGameManager.AddSaveable(Bytes, Pos, DTO);
        }

        return Bytes.ToArray();
    }

    public void SetData(NativeArray<byte> Bytes)
    {
        if (!Game.TryGetService(out CardFactory CardFactory))
            return;

        for (int i = 0; i < Cards.Count; i++)
        {
            DestroyImmediate(Cards[i].gameObject);
        }
        Cards = new();

        int Pos = sizeof(int);
        Pos = SaveGameManager.GetInt(Bytes, Pos, out int Count);
        for (int i = 0; i < Count; i++)
        {
            CardDTO DTO = CardDTO.CreateForSaveable(Bytes, Pos);
            Pos = SaveGameManager.SetSaveable(Bytes, Pos, DTO);
            CardFactory.CreateCardFromDTO(DTO, i, transform, AddCard);
        }
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

    public virtual void Load() { }

    public bool ShouldLoadWithLoadedSize() { return true; }

    public void Reset()
    {
        DeleteAllCardsConditionally((Card) =>
        {
            return true;
        });
    }

    public abstract bool ShouldCardsBeDisplayed();
    public abstract float GetCardSize();

    public List<Card> Cards = new List<Card>();
    public TMPro.TextMeshProUGUI Text;
}
