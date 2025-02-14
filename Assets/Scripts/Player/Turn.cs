using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

        GameplayAbilitySystem._OnBehaviourRegistered += OnPlayerBehaviourRegistered;

        _OnInit?.Invoke(this);
    }

    protected override void StopServiceInternal() {
        gameObject.SetActive(false);
        gameObject.SetActive(false);
    }

    public void OnDestroy()
    {
        GameplayAbilitySystem._OnBehaviourRegistered -= OnPlayerBehaviourRegistered;
    }

    private void OnPlayerBehaviourRegistered(GameplayAbilityBehaviour Behaviour)
    {
        if (!Behaviour.gameObject.name.ToLower().Equals("player"))
            return;

        Game.RunAfterServiceInit((AmberService Ambers) =>
        {
            UpdateTurnCounter();
        });
    }

    public void NextTurn() {
        if (!bIsEnabled || !IsInit)
            return;

        if (HasMalaisedEntities())
            return;

        if (HasIdleEntities())
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
        string Type = Unit != null ? Unit.GetUType().ToString() :
                    Worker != null ? "Worker" : "Unit";
        if (MalaisedEntity == null)
            return false;

        ConfirmScreen.Show("You have a " + Type + " that will be destroyed this turn by the malaise!", ExecuteNextTurn, ShowUnitToHighlight, "Show");
        return true;
    }

    private bool HasIdleEntities()
    {
        if (!Game.TryGetServices(out Units Units, out Workers Workers))
            return false;

        Units.TryGetIdleEntity(out TokenizedUnitEntity Unit);
        Workers.TryGetIdleEntity(out StarvableUnitEntity Worker);

        IdleEntity = Unit != null ? Unit :
                    Worker != null ? Worker : null;
        if (IdleEntity == null)
            return false;

        string Type = Unit != null ? Unit.GetUType().ToString() :
                    Worker != null ? "Worker" : "Unit";
        ConfirmScreen.Show("You have an idle " + Type, ExecuteNextTurn, ShowUnitToHighlight, "Show");
        return true;
    }

    private void ShowUnitToHighlight()
    {
        Location TargetLocation = null;
        ScriptableEntity TargetEntity = MalaisedEntity != null ? MalaisedEntity : IdleEntity;
        if (TargetEntity is WorkerEntity)
        {
            WorkerEntity MalaisedWorker = TargetEntity as WorkerEntity;
            BuildingEntity AssignedBuilding = MalaisedWorker.GetAssignedBuilding();
            TargetLocation = AssignedBuilding != null ? AssignedBuilding.GetLocationAboutToBeMalaised() : null;
        }
        if (TargetEntity is TokenizedUnitEntity)
        {
            TargetLocation = (TargetEntity as TokenizedUnitEntity).GetLocations().GetMainLocation();
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
        Stockpile.ProduceUnits();
        Stockpile.ResearchTurn();
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

        UpdateTurnCounter();
        CheckForGameOver();
    }

    private void UpdateTurnCounter()
    {
        bool bHasMaxTurn = HasMaxTurn();
        TurnCounterText.gameObject.SetActive(bHasMaxTurn);
        TurnCounterImage.SetActive(bHasMaxTurn);
        if (!bHasMaxTurn)
            return;

        TurnCounterText.text = TurnNr + "/" + GetTotalMaxTurns();
    }

    private bool HasMaxTurn()
    {
        if (!Game.TryGetService(out AmberService Amber))
            return false;

        if (!Amber.IsUnlocked())
            return false;

        // no modifier applied 
        return Amber.Infos[AttributeType.AmberMaxTurns].CurrentValue != 0;
    }

    private int GetTotalMaxTurns()
    {
        int TurnSubtract = (int)AttributeSet.Get()[AttributeType.AmberMaxTurns].CurrentValue;
        return MaxTurns + TurnSubtract;
    }


    private void OnPause()
    {
        bIsEnabled = false;
    }

    private void OnResume()
    {
        bIsEnabled = true;
    }

    private void CheckForGameOver()
    {
        if (!HasMaxTurn())
            return;

        if (TurnNr < GetTotalMaxTurns())
            return;

        Game.Instance.GameOver("You ran out of turns!");
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
    protected override void ResetInternal()
    {
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
    private ScriptableEntity IdleEntity;

    public int TurnNr = 0;

    public AbandonScreen AbandonScreen;
    public GameObject TurnCounterImage;
    public TMPro.TextMeshProUGUI TurnCounterText;

    public delegate void OnTurnEnd();
    public static OnTurnEnd _OnTurnEnd;

    public static ActionList<int> _OnTurnEnded = new();
    public static ActionList<int> _OnRunAbandoned = new();

    public static int MaxTurns = 50;
}
