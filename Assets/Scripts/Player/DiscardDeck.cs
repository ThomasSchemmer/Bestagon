using System.Collections;
using UnityEngine;

/**
 * Holds all cards that have been played by the player and not yet shuffled
 */
public class DiscardDeck : CardCollection {

    public override void AddCard(Card Card) {
        base.AddCard(Card);
        Text.text = "" + Cards.Count;
        Card.Show(Card.Visibility.Hidden);
    }

    protected override void StartServiceInternal()
    {
        Game.RunAfterServiceInit((RelicService RelicService) =>
        {
            base.StartServiceInternal();
            Text.text = "" + Cards.Count;
            _OnInit?.Invoke(this);
        });
    }


    public override bool ShouldCardsBeDisplayed()
    {
        return false;
    }

    public override float GetCardSize()
    {
        return 0;
    }

    public override Card.CardState GetState()
    {
        return Card.CardState.Played;
    }
    public override bool ShouldUpdateCardState()
    {
        return true;
    }

    public static Vector3 RootPosition = new Vector3(-20, -200, 0);
    public static Vector3 Offset = new Vector3(0, -15, 0);

}
