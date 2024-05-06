using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/**
 * Helper class to wrap all templated selectors into one easily accesible UI element / gameservice
 */
public class Selectors : GameService {
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
    }

    protected override void StopServiceInternal() {}

    public void Update() {
        if (!IsEnabled)
            return;

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

    public Card GetSelectedCard() {
        return CardSelector.Selected;
    }

    public HexagonVisualization GetSelectedHexagon() {
        return HexagonSelector.Selected;
    }

    public UIElement GetSelectedUIElement() 
    {
        return UISelector.Selected;
    }

    public void SelectHexagon(HexagonVisualization Vis) {
        HexagonSelector.Select(Vis);
    }

    public void DeselectHexagon() { 
        HexagonSelector.Deselect(true);
    }

    public void ForceDeselect() {
        CardSelector.Deselect(true);
        CardSelector.Deselect(false);
        HexagonSelector.Deselect(true);
        HexagonSelector.Deselect(false);
        UISelector.Deselect(true);
        UISelector.Deselect(false);
    }

    public void ShowTooltip(Selectable Selectable, bool bShow)
    {
        ToolTipScreen.Show(Selectable, bShow);
    }

    public Selector<Card> CardSelector;
    public Selector<HexagonVisualization> HexagonSelector;
    public Selector<UIElement> UISelector;
    public ToolTipScreen ToolTipScreen;

    private bool IsEnabled = true;
}
