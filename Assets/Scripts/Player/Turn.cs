using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Turn : GameService, IQuestRegister<int>
{
    public void OnEnable()
    {
        Game.Instance._OnPause += OnPause;
        Game.Instance._OnResume += OnResume;
    }
    protected override void StartServiceInternal()
    {
        CardHand = Game.GetService<CardHand>();
        CardDeck = Game.GetService<CardDeck>();
        DiscardDeck = Game.GetService<DiscardDeck>();
        Stockpile = Game.GetService<Stockpile>();
        MiniMap = Game.GetService<MiniMap>();
        Units = Game.GetService<Units>();
        CloudRenderer = Game.GetService<CloudRenderer>();
        Selectors = Game.GetService<Selectors>();
        Quests = Game.GetService<QuestService>();

        gameObject.SetActive(true);
        gameObject.SetActive(true);
        _OnInit?.Invoke(this);
    }

    protected override void StopServiceInternal() {
        gameObject.SetActive(false);
        gameObject.SetActive(false);
    }

    public void NextTurn() {
        if (!bIsEnabled || !IsInit)
            return;

        if (HasMalaisedEntities())
            return;

        ExecuteNextTurn();
    }

    private bool HasMalaisedEntities()
    {
        if (!Game.TryGetServices(out Units Units, out Workers Workers))
            return false;

        Units.TryGetEntityToBeMalaised(out TokenizedUnitEntity Unit);
        Workers.TryGetEntityToBeMalaised(out StarvableUnitEntity Worker);

        MalaisedEntity = Unit != null ? Unit :
                    Worker != null ? Worker : null;
        if (MalaisedEntity == null)
            return false;

        ConfirmScreen.Show("You have a unit that will be destroyed this turn by the malaise!", ExecuteNextTurn, ShowMalaisedUnit, "Show");
        return true;
    }

    private void ShowMalaisedUnit()
    {
        Location TargetLocation = null;
        if (MalaisedEntity is WorkerEntity)
        {
            WorkerEntity MalaisedWorker = MalaisedEntity as WorkerEntity;
            BuildingEntity AssignedBuilding = MalaisedWorker.GetAssignedBuilding();
            TargetLocation = AssignedBuilding != null ? AssignedBuilding.GetLocation() : null;
        }
        if (MalaisedEntity is TokenizedUnitEntity)
        {
            TargetLocation = (MalaisedEntity as TokenizedUnitEntity).GetLocation();
        }

        if (TargetLocation == null)
            return;

        if (!Game.TryGetService(out CameraController CameraController))
            return;

        CameraController.TeleportTo(TargetLocation.WorldLocation);
    }

    private void ExecuteNextTurn()
    {
        MalaisedEntity = null;
        MessageSystemScreen.DeleteAllMessages();
        Stockpile.GenerateResources();
        Stockpile.ProduceWorkers();
        TurnNr++;
        Units.RefreshEntities();
        MoveCards();
        CloudRenderer.SpreadMalaise();

        UpdateSelection();

        MiniMap.FillBuffer();
        Selectors.DeselectCard();
        Quests.CheckForQuestsToUnlock();

        _OnTurnEnd?.Invoke();
        _OnTurnEnded.ForEach(_ => _.Invoke(TurnNr));
    }


    private void OnPause()
    {
        bIsEnabled = false;
    }

    private void OnResume()
    {
        bIsEnabled = true;
    }

    private void MoveCards() {
        CardHand.MoveAllCardsTo(DiscardDeck);

        CardHand.HandleDelayedFilling();

        // We already moved enough cards, dont need to refill
        if (CardHand.Cards.Count >= CardHand.GetMaxHandCardCount())
            return;
        
        FillCardDeck();
        CardHand.HandleDelayedFilling();
    }

    private void FillCardDeck() {
        List<Card> Cards = new();
        // first remove every card
        Card CurrentCard = DiscardDeck.RemoveCard();
        while (CurrentCard != null) {
            Cards.Add(CurrentCard);
            CurrentCard = DiscardDeck.RemoveCard();
        }

        // then shuffle
        for (int i = 0; i < Cards.Count; i++) {
            int TargetIndex = Random.Range(i, Cards.Count);
            Card Temp = Cards[i];
            Cards[i] = Cards[TargetIndex];
            Cards[TargetIndex] = Temp;  
        }

        // and then add into the card deck
        for (int i = 0; i < Cards.Count; i++) {
            CardDeck.AddCard(Cards[i]);
        }
    }

    private void UpdateSelection() {
        if (!Game.TryGetService(out Selectors Selector))
            return;

        // this triggers all visualizations for the selected hex
        HexagonVisualization Hex = Selector.GetSelectedHexagon();
        if (Hex == null) 
            return;

        Selector.SelectHexagon(Hex);
    }

    public void InvokeOnRunAbandoned()
    {
        _OnRunAbandoned.ForEach(_ => _.Invoke(0));
    }

    public void Show(bool bShow)
    {
        gameObject.SetActive(bShow);
    }

    public AbandonScreen GetAbandonScreen()
    {
        return AbandonScreen;
    }

    private bool bIsEnabled = true;
    private CardHand CardHand;
    private CardDeck CardDeck;
    private DiscardDeck DiscardDeck;
    private Stockpile Stockpile;
    private MiniMap MiniMap;
    private Units Units;
    private CloudRenderer CloudRenderer;
    private Selectors Selectors;
    private QuestService Quests;

    private ScriptableEntity MalaisedEntity;

    public int TurnNr = 0;

    public AbandonScreen AbandonScreen;

    public delegate void OnTurnEnd();
    public static OnTurnEnd _OnTurnEnd;

    public static ActionList<int> _OnTurnEnded = new();
    public static ActionList<int> _OnRunAbandoned = new();
}
