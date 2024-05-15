using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/**
 * Helper class to wrap all templated selectors into one easily accesible UI element / gameservice
 */
public class Selectors : GameService
{

    protected override void StartServiceInternal()
    {
        CardSelector = new Selector<Card>();
        HexagonSelector = new Selector<HexagonVisualization>();
        UISelector = new Selector<UIElement>(true);

        CardSelector.Layer = "Card";
        HexagonSelector.Layer = "Hexagon";
        UISelector.Layer = "UI";
        Game.Instance._OnPause += OnPause;
        Game.Instance._OnResume += OnResume;

        _OnInit?.Invoke();
    }

    protected override void StopServiceInternal() { }

    public void Update()
    {
        if (!IsEnabled)
            return;

        if (Input.GetMouseButtonDown(0)) {
            Debug.Log("");
        }


        if (UISelector.RayCast())
            return;

        if (CardSelector.RayCast())
            return;

        HexagonSelector.RayCast();
    }

    private void OnPause()
    {
        IsEnabled = false;
        ForceDeselect();
    }

    private void OnResume()
    {
        IsEnabled = true;
    }

    public Card GetSelectedCard()
    {
        return CardSelector.Selected;
    }

    public HexagonVisualization GetSelectedHexagon()
    {
        return HexagonSelector.Selected;
    }

    public UIElement GetSelectedUIElement()
    {
        return UISelector.Selected;
    }

    public void SelectHexagon(HexagonVisualization Vis)
    {
        HexagonSelector.Select(Vis);
    }

    public void DeselectHexagon()
    {
        HexagonSelector.DeSelect(true);
    }

    public void ForceDeselect()
    {
        DeselectCard();
        DeselectHexagon();
        UISelector.DeSelect(true);
        UISelector.DeSelect(false);
    }

    public void DeselectCard()
    {
        CardSelector.DeSelect(true);
        CardSelector.DeSelect(false);
    }

    public HexagonVisualization GetHoveredHexagon()
    {
        return HexagonSelector.Hovered;
    }

    public void ShowTooltip(ISelectable Selectable, bool bShow)
    {
        ToolTipScreen.Show(Selectable, bShow);
    }

    public Selector GetSelectorByType(ISelectable Selectable)
    {
        if (Selectable is Card)
            return CardSelector;

        if (Selectable is UIElement)
            return UISelector;

        if (Selectable is HexagonVisualization)
            return HexagonSelector;

        return null;
    }

    public Selector<Card> CardSelector;
    public Selector<HexagonVisualization> HexagonSelector;
    public Selector<UIElement> UISelector;
    public ToolTipScreen ToolTipScreen;

    private bool IsEnabled = true;
}