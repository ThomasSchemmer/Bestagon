﻿using System.Collections;
using UnityEngine;

public class DiscardDeck : CardCollection {

    public override void AddCard(Card Card) {
        base.AddCard(Card);
        //Card.gameObject.SetActive(false);
        //Card.transform.localPosition = RootPosition + Offset * Cards.Count;
        //Card.gameObject.layer = 0;
        Text.text = "" + Cards.Count;
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

    public override void OnLoaded()
    {
        base.OnLoaded();

    }

    public override bool ShouldCardsBeDisplayed()
    {
        return false;
    }

    public override float GetCardSize()
    {
        return 0;
    }

    public static Vector3 RootPosition = new Vector3(-20, -200, 0);
    public static Vector3 Offset = new Vector3(0, -15, 0);

}
