using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardFactory : GameService
{
    private class DelayedCardInfo
    {
        /** on newly creating a card (and not loading it from a savegame) the type is set
         * otherwise the CardDTO. The buildingdata cant be created otherwise
         */
        public BuildingConfig.Type? Type;
        public CardDTO DTO;
        public int Index;
        public Transform Parent;
        public Action<Card> Callback;

        public DelayedCardInfo(CardDTO DTO, int Index, Transform Parent, Action<Card> Callback)
        {
            this.Type = null;
            this.DTO = DTO;
            this.Index = Index;
            this.Parent = Parent;
            this.Callback = Callback;
        }

        public DelayedCardInfo(BuildingConfig.Type Type, int Index, Transform Parent, Action<Card> Callback)
        {
            this.Type = Type;
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

    public void CreateCardFromDTO(CardDTO DTO, int Index, Transform Parent, Action<Card> Callback)
    {
        DelayedCardInfo Info = new(DTO, Index, Parent, Callback);
        CreateCard(Info);
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
        GameObject CardPrefab = Resources.Load("UI/Card") as GameObject;
        GameObject GO = Instantiate(CardPrefab, Info.Parent);
        Card Card = InitDelayedBuildingCard(GO, Info);
        Info.Callback.Invoke(Card);
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

        if (!Game.TryGetService(out TileFactory TileFactory))
            return null;

        BuildingData BuildingData = TileFactory.CreateFromType((BuildingConfig.Type)Info.Type);
        if (BuildingData == null)
        {
            MessageSystem.CreateMessage(Message.Type.Error, "No valid building data was found for type " + Info.Type);
        }
        return BuildingData;
    }

    protected override void StartServiceInternal()
    {
        Game.RunAfterServicesInit((IconFactory IconFactory, TileFactory TileFactory) =>
        {
            CreateDelayedCards();
            _OnInit?.Invoke();
        });
    }

    protected override void StopServiceInternal() {}
}
