using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardFactory : GameService
{
    private class DelayedCardInfo
    {
        public CardDTO DTO;
        public int Index;
        public Transform Parent;
        public Action<Card> Callback;
        public Action<Card, int> CallbackIndex;

        public DelayedCardInfo(CardDTO DTO, int Index, Transform Parent, Action<Card> Callback)
        {
            this.DTO = DTO;
            this.Index = Index;
            this.Parent = Parent;
            this.Callback = Callback;
        }

        public DelayedCardInfo(CardDTO DTO, int Index, Transform Parent, Action<Card, int> Callback)
        {
            this.DTO = DTO;
            this.Index = Index;
            this.Parent = Parent;
            this.CallbackIndex = Callback;
        }

        public DelayedCardInfo(BuildingConfig.Type Type, int Index, Transform Parent, Action<Card> Callback)
        {
            if (!Game.TryGetService(out MeshFactory TileFactory))
                return;

            this.DTO = BuildingCardDTO.CreateFromBuildingData(TileFactory.CreateDataFromType(Type));
            this.Index = Index;
            this.Parent = Parent;
            this.Callback = Callback;
        }

        public DelayedCardInfo(BuildingConfig.Type Type, int Index, Transform Parent, Action<Card, int> Callback)
        {
            if (!Game.TryGetService(out MeshFactory TileFactory))
                return;

            this.DTO = BuildingCardDTO.CreateFromBuildingData(TileFactory.CreateDataFromType(Type));
            this.Index = Index;
            this.Parent = Parent;
            this.CallbackIndex = Callback;
        }

        public DelayedCardInfo(EventData.EventType Type, int Index, Transform Parent, Action<Card> Callback)
        {
            this.DTO = EventCardDTO.CreateFromEventData(EventData.CreateRandom(Type));
            this.Index = Index;
            this.Parent = Parent;
            this.Callback = Callback;
        }

        public DelayedCardInfo(EventData.EventType Type, int Index, Transform Parent, Action<Card, int> Callback)
        {
            this.DTO = EventCardDTO.CreateFromEventData(EventData.CreateRandom(Type));
            this.Index = Index;
            this.Parent = Parent;
            this.CallbackIndex = Callback;
        }

        public DelayedCardInfo(UnitData.UnitType UnitType, int Index, Transform Parent, Action<Card> Callback)
        {
            GrantUnitEventData EventData = ScriptableObject.CreateInstance<GrantUnitEventData>();
            EventData.GrantedType = UnitType;
            this.DTO = EventCardDTO.CreateFromEventData(EventData);
            this.Index = Index;
            this.Parent = Parent;
            this.Callback = Callback;
        }
    }

    private List<DelayedCardInfo> CardsToCreate = new();

    public void CreateCard(BuildingConfig.Type Type, int Index, Transform Parent, Action<Card> Callback)
    {
        DelayedCardInfo Info = new(Type, Index, Parent, Callback);
        CreateCard(Info);
    }

    public void CreateCard(BuildingConfig.Type Type, int Index, Transform Parent, Action<Card, int> Callback)
    {
        DelayedCardInfo Info = new(Type, Index, Parent, Callback);
        CreateCard(Info);
    }

    public void CreateCard(EventData.EventType Type, int Index, Transform Parent, Action<Card> Callback)
    {
        DelayedCardInfo Info = new(Type, Index, Parent, Callback);
        CreateCard(Info);
    }
    public void CreateCard(EventData.EventType Type, int Index, Transform Parent, Action<Card, int> Callback)
    {
        DelayedCardInfo Info = new(Type, Index, Parent, Callback);
        CreateCard(Info);
    }

    public void CreateCard(UnitData.UnitType Type, int Index, Transform Parent, Action<Card> Callback)
    {
        DelayedCardInfo Info = new(Type, Index, Parent, Callback);
        CreateCard(Info);
    }

    public void CreateCardFromDTO(CardDTO DTO, int Index, Transform Parent, Action<Card> Callback)
    {
        DelayedCardInfo Info = new(DTO, Index, Parent, Callback);
        CreateCard(Info);
    }

    public void CreateCardFromDTO(CardDTO DTO, int Index, Transform Parent, Action<Card, int> Callback)
    {
        DelayedCardInfo Info = new(DTO, Index, Parent, Callback);
        CreateCard(Info);
    }

    public void CloneCard(Card Card, Action<Card> Callback)
    {
        CardDTO DTO = CardDTO.CreateFromCard(Card);
        CreateCardFromDTO(DTO, 0, null, Callback);
    }

    private void CreateCard(DelayedCardInfo Info)
    {
        CardsToCreate.Add(Info);

        if (IsInit)
        {
            CreateDelayedCards();
        }
    }

    private void CreateDelayedCards()
    {
        foreach (DelayedCardInfo Info in CardsToCreate)
        {
            CreateDelayedCard(Info);
        }
        CardsToCreate.Clear();
    }

    private void CreateDelayedCard(DelayedCardInfo Info)
    {
        GameObject CardPrefab = Resources.Load("UI/Cards/Card") as GameObject;
        GameObject GO = Instantiate(CardPrefab, Info.Parent);
        Card Card = InitDelayedCardByType(GO, Info);
        Info.Callback?.Invoke(Card);
        Info.CallbackIndex?.Invoke(Card, Info.Index);
    }

    private Card InitDelayedCardByType(GameObject CardObject, DelayedCardInfo Info)
    {
        if (Info.DTO is BuildingCardDTO)
            return InitDelayedBuildingCard(CardObject, Info);

        if (Info.DTO is EventCardDTO)
            return InitDelayedEventCard(CardObject, Info);

        return null;
    }

    private Card InitDelayedEventCard(GameObject CardObject, DelayedCardInfo Info)
    {
        EventData EventData = (Info.DTO as EventCardDTO).EventData;

        CardObject.name = "Card " + EventData.Type.ToString();
        EventCard Card = CardObject.AddComponent<EventCard>();
        Card.Init(EventData, Info.Index);

        return Card;
    }

    private Card InitDelayedBuildingCard(GameObject CardObject, DelayedCardInfo Info)
    {
        BuildingData BuildingData = GetBuildingDataFromInfo(Info);

        CardObject.name = "Card " + BuildingData.BuildingType;
        BuildingCard Card = CardObject.AddComponent<BuildingCard>();
        Card.Init(BuildingData, Info.Index);

        return Card;
    }

    private BuildingData GetBuildingDataFromInfo(DelayedCardInfo Info)
    {
        if (Info.DTO != null && Info.DTO is BuildingCardDTO)
            return (Info.DTO as BuildingCardDTO).BuildingData;

        return null;
    }

    protected override void StartServiceInternal()
    {
        Game.RunAfterServicesInit((IconFactory IconFactory, MeshFactory TileFactory) =>
        {
            CreateDelayedCards();
            _OnInit?.Invoke(this);
        });
    }

    protected override void StopServiceInternal() {}
}
