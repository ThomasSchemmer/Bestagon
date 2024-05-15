using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class CardHand : CardCollection
{

    protected override void StartServiceInternal()
    {
        Game.RunAfterServicesInit((SaveGameManager Manager, Unlockables Unlockables) => {

            base.StartServiceInternal();

            // cards will be loaded instead of newly generated
            if (Manager.HasDataFor(ISaveable.SaveGameType.CardHand))
                return;

            Game.RunAfterServicesInit((CardFactory CardFactory, CardDeck Deck) =>
            {
                Cards = new List<Card>();
                HandleDelayedFilling();
                _OnInit?.Invoke();
            });
            
        });
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
        HandleDelayedFilling();
        

        _OnInit?.Invoke();
    }

    private void HandleDelayedFilling()
    {
        if (!Game.IsIn(Game.GameState.Game) && !Game.IsIn(Game.GameState.GameMenu))
            return;

        Game.RunAfterServicesInit((CardDeck Deck, CardFactory CardFactory) =>
        {
            int TargetAmount = AMOUNT_HANDCARDS_START;
            int Amount = Mathf.Max(TargetAmount - Cards.Count, 0);
            Amount = Mathf.Min(Amount, Deck.Cards.Count);

            for (int i = 0; i < Amount; i++)
            {
                Card Card = Deck.RemoveCard();
                AddCard(Card);
            }
        });
    }

    public static Vector3 NormalPosition = new Vector3(0, -500, 0);
    public static Vector3 HoverPosition = new Vector3(0, -350, 0);
    public static int AMOUNT_HANDCARDS_MAX = 5;
    public static int AMOUNT_HANDCARDS_START = 3;
}
