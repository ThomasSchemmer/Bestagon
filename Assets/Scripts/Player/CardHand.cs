using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.XR;

public class CardHand : CardCollection, IQuestRegister<Card>
{

    protected override void StartServiceInternal()
    {
        Game.RunAfterServicesInit((SaveGameManager Manager, BuildingService BuildingService) => {

            base.StartServiceInternal();

            // cards will be loaded instead of newly generated
            if (Manager.HasDataFor(ISaveableService.SaveGameType.CardHand))
                return;

            Game.RunAfterServiceInit((CardDeck Deck) =>
            {
                Cards = new List<Card>();
                HandleDelayedFilling(Deck);
                _OnInit?.Invoke(this);
            });
            
        });
    }

    public override void Sort()
    {
        base.Sort();
        Sort(false);
    }

    public void Sort(bool IsACardHovered) {
        transform.localPosition = IsACardHovered ? HoverPosition : NormalPosition;
        int Count = Cards.Count;
        int i = 0;

        int AreaOffset = (int)(Area.x / (Count + 1));
        float MaxXOffset = Mathf.Min(AreaOffset, CardOffset);
        MaxXOffset *= Count / 2f;

        foreach(Card Card in Cards) {
            Card.SetIndex(Count - 1 - Card.GetIndex());

            float TempIndex = Count == 1 ? 0.5f : (float)i / (Count - 1);
            float MappedIndex = Map(TempIndex, 0, 1, -1, 1);
            float YIndex = 1 - Mathf.Abs(MappedIndex);

            float YOffset = YIndex * Area.y;
            float SelectYOffset = Card.IsSelected() ? Card.SelectOffset : 0;
            float XOffset = MappedIndex * MaxXOffset;

            Card.transform.localPosition = new Vector3(XOffset, YOffset + SelectYOffset, 0);
            Card.transform.rotation = Quaternion.Euler(0, 0, MappedIndex * MaxAngle);
            Card.name = "Card " + i;
            i++;
        }
        Canvas.ForceUpdateCanvases();
    }


    private float Map(float value, float from1, float to1, float from2, float to2)
    {
        return (value - from1) / Mathf.Round(to1 - from1) * (to2 - from2) + from2;
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
        _OnCardPlayed.ForEach(_ => _.Invoke(Card));
    }

    public override void AddCard(Card Card) {
        base.AddCard(Card);
        Card.SetCanBeHovered(true);
        Card.gameObject.SetActive(true);
        Card.gameObject.layer = LayerMask.NameToLayer(Selectors.UILayerName);
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
        Game.RunAfterServiceInit((CardDeck Deck) =>
        {
            HandleDelayedFilling(Deck);
            _OnInit?.Invoke(this);
        });
    }

    public void HandleDelayedFilling(CardDeck Deck)
    {
        int TargetAmount = GetMaxHandCardCount();
        int Amount = Mathf.Max(TargetAmount - Cards.Count, 0);
        Amount = Mathf.Min(Amount, Deck.Cards.Count);

        List<Card> CardsToMove = new();
        CardsToMove.AddRange(Deck.Cards.GetRange(0, Amount));

        Deck.MoveCardsTo(CardsToMove, this);
    }

    public static int GetMaxHandCardCount()
    {
        return (int)AttributeSet.Get()[AttributeType.MaxAmountHandCards].CurrentValue;
    }

    public override bool ShouldCardsBeDisplayed()
    {
        return true;
    }

    public override float GetCardSize()
    {
        return 1;
    }

    public static Vector3 NormalPosition = new Vector3(0, -500, 0);
    public static Vector3 HoverPosition = new Vector3(0, -400, 0);
    public static Vector3 Area = new Vector3(1200, 25, 0);
    public static float MaxAngle = -10;
    public static float CardOffset = 175;

    public static ActionList<Card> _OnCardPlayed = new();
}
