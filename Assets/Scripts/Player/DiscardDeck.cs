using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DiscardDeck : CardCollection {
    public void Start() {
        Instance = this;
        Text.text = "" + Cards.Count;
    }

    public override void AddCard(Card Card) {
        base.AddCard(Card);
        Card.gameObject.SetActive(false);
        Card.transform.localPosition = RootPosition + Offset * Cards.Count;
        Card.gameObject.layer = 0;
        Text.text = "" + Cards.Count;
    }

    public static DiscardDeck Instance;

    public static Vector3 RootPosition = new Vector3(-20, -200, 0);
    public static Vector3 Offset = new Vector3(0, -15, 0);

}
