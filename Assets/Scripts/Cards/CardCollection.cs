using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEditor;
using UnityEngine;

public abstract class CardCollection : GameService, ISaveable
{
    public virtual void AddCard(Card Card) {
        Cards.Add(Card);
        Card.transform.SetParent(transform, false);
        Card.SetCanBeHovered(false);
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

    public void MoveAllCardsTo(CardCollection OtherCollection)
    {
        while (Cards.Count > 0)
        {
            Card Card = RemoveCard();
            OtherCollection.AddCard(Card);
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

    public virtual void RemoveCard(Card Card) {
        Cards.Remove(Card);
        if (Text) {
            Text.text = "" + Cards.Count;
        }
    }

    protected Card CreateRandomCard(int i) {
        if (!Game.TryGetService(out TileFactory BuildingFactory))
            return null;

        int TypeCount = BuildingFactory.GetUnlockedBuildings().Count;
        BuildingConfig.Type Type = (BuildingConfig.Type)(1 << UnityEngine.Random.Range(1, TypeCount));
        return Card.CreateCard(Type, i, transform);
    }

    protected override void StartServiceInternal()
    {
        gameObject.SetActive(true);
    }

    protected override void StopServiceInternal() { 
        gameObject.SetActive(false);
    }

    public int GetSize()
    {
        //byte count and amount of cards
        int Size = sizeof(int) * 2;
        Size += Cards.Count * CardDTO.GetStaticSize();
        return Size;
    }

    public byte[] GetData()
    {
        NativeArray<byte> Bytes = new(GetSize(), Allocator.Temp);
        int Pos = 0;

        Pos = SaveGameManager.AddInt(Bytes, Pos, GetSize());
        Pos = SaveGameManager.AddInt(Bytes, Pos, Cards.Count);

        foreach (Card Card in Cards)
        {
            CardDTO DTO = new(Card);
            Pos = SaveGameManager.AddSaveable(Bytes, Pos, DTO);
        }

        return Bytes.ToArray();
    }

    public void SetData(NativeArray<byte> Bytes)
    {
        for (int i = 0; i < Cards.Count; i++)
        {
            Destroy(Cards[i].gameObject);
        }
        Cards = new();

        int Pos = sizeof(int);
        Pos = SaveGameManager.GetInt(Bytes, Pos, out int Count);
        for (int i = 0; i < Count; i++)
        {
            CardDTO DTO = new();
            Pos = SaveGameManager.SetSaveable(Bytes, Pos, DTO);
            Card Card = Card.CreateCardFromDTO(DTO, i, transform);
            AddCard(Card);
        }
    }

    public void RefreshAllUsages()
    {
        foreach (Card Card in Cards)
        {
            Card.RefreshUsage();
        }
    }

    public void RefreshAllUsedUps()
    {
        foreach (Card Card in Cards)
        {
            Card.RefreshUsedUp();
        }
    }

    public virtual void Load() { }

    public bool ShouldLoadWithLoadedSize() { return true; }

    public List<Card> Cards = new List<Card>();
    public TMPro.TextMeshProUGUI Text;
}
