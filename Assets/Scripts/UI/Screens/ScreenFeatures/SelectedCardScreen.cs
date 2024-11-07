using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectedCardScreen : ScreenFeatureGroup<BuildingCard>
{

    public void Start()
    {
        Game.RunAfterServiceInit((Selectors Selectors) =>
        {
            Selector = Selectors.CardSelector;
            Selector.OnItemSelected += Show;
            Selector.OnItemDeSelected += Hide;
        });

        Init();
        Hide();

        Game.Instance._OnPause += Hide;

    }

    public void Show(Card Card)
    {
        if (Card is not BuildingCard)
            return;

        SelectedCard = Card as BuildingCard;
        Show();
    }

    public void Show()
    {
        ShowFeatures();
    }

    public void Hide()
    {
        SelectedCard = null;
        HideFeatures();
    }

    public override BuildingCard GetFeatureObject()
    {
        return SelectedCard;
    }

    public override bool HasFeatureObject()
    {
        return SelectedCard != null;
    }

    public Selector<Card> Selector;

    private BuildingCard SelectedCard;
}
