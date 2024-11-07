

using UnityEngine;

/**
 * Helper class to wrap all templated selectors into one easily accesible UI element / gameservice
 */
public class Selectors : GameService
{

    protected override void StartServiceInternal()
    {
        CardSelector = new Selector<Card>(true);
        HexagonSelector = new Selector<HexagonVisualization>();
        UISelector = new Selector<UIElement>(true);

        CardSelector.Layer = UILayerName;
        HexagonSelector.Layer = "Hexagon";
        UISelector.Layer = UILayerName;
        Game.Instance._OnPause += OnPause;
        Game.Instance._OnResume += OnResume;
        Game.Instance._OnPopup += OnPopup;

        _OnInit?.Invoke(this);
    }

    protected override void StopServiceInternal() { }

    public void Update()
    {
        if (!bIsEnabled || !IsInit)
            return;

        if (UISelector.RayCast())
            return;

        if (bIsPopuped)
            return;

        if (CardSelector.RayCast())
            return;

        HexagonSelector.RayCast();
    }

    private void OnPause()
    {
        bIsEnabled = false;
        ForceDeselect();
    }

    private void OnPopup(bool bIsOpen)
    {
        bIsPopuped = bIsOpen;
        if (bIsPopuped)
        {
            ForceDeselect();
        }
    }

    private void OnResume()
    {
        bIsEnabled = true;
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

    public void ReHoverHexagon()
    {
        HexagonVisualization Vis = HexagonSelector.Hovered;
        if (Vis == null)
            return;

        HexagonSelector.DeSelect(false);
        HexagonSelector.Hover(Vis);
    }

    public void ForceDeselect()
    {
        DeselectCard();
        DeselectHexagon();
        DeselectUI();
        HideTooltip();
    }

    public void DeselectCard()
    {
        CardSelector.DeSelect(true);
        CardSelector.DeSelect(false);
    }

    public void DeselectUI()
    {
        UISelector.DeSelect(false);
        UISelector.DeSelect(true);
    }

    public HexagonVisualization GetHoveredHexagon()
    {
        return HexagonSelector.Hovered;
    }

    public void ShowTooltip(ISelectable Selectable, bool bShow)
    {
        ToolTipScreen.Show(Selectable, bShow);
    }

    public void HideTooltip()
    {
        ToolTipScreen.Show(null, false);
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

    private bool bIsEnabled = true;
    private bool bIsPopuped = false;

    public static string UILayerName = "UI";
}