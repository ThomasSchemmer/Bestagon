﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DiscardDeck : MonoBehaviour
{
    public void Start() {
        Instance = this;
    }

    private void AddCardInternal(Card Card) {
        Cards.Add(Card);
        Card.transform.SetParent(transform, false);
        Card.transform.localPosition = RootPosition + Offset * Cards.Count;
        Card.gameObject.layer = 0;
    }

    public static void AddCard(Card Card) {
        if (!Instance)
            return;

        Instance.AddCardInternal(Card);
    }

    public List<Card> Cards;

    public static DiscardDeck Instance;

    public static Vector3 RootPosition = new Vector3(-20, -200, 0);
    public static Vector3 Offset = new Vector3(0, -15, 0);

}
