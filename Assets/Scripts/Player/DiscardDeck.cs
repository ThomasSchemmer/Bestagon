using System.Collections;
using System.Collections.Generic;
using System.Transactions;
using UnityEngine;

public class DiscardDeck : CardCollection {

    public override void AddCard(Card Card) {
        base.AddCard(Card);
        Card.gameObject.SetActive(false);
        Card.transform.localPosition = RootPosition + Offset * Cards.Count;
        Card.gameObject.layer = 0;
        Text.text = "" + Cards.Count;
    }

    protected override void StartServiceInternal()
    {
        base.StartServiceInternal();
        Text.text = "" + Cards.Count;
    }

    public override void Load()
    {
        base.Load();

    }

    public static Vector3 RootPosition = new Vector3(-20, -200, 0);
    public static Vector3 Offset = new Vector3(0, -15, 0);

}
