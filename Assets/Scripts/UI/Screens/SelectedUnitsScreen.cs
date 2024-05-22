using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectedUnitsScreen : ScreenFeatureGroup<UnitData>
{
    public Selector<HexagonVisualization> Selector;

    private UnitData SelectedUnit;
    private HexagonVisualization SelectedHex;

    void Start()
    {
        Game.RunAfterServiceInit((Selectors Selectors) =>
        {
            Selector = Selectors.HexagonSelector;
            Selector.OnItemSelected += Show;
            Selector.OnItemDeSelected += Hide;
        });

        Init();
        Hide();

        Game.Instance._OnPause += Hide;
    }

    public void Show(HexagonVisualization Hex)
    {
        Selector.OnItemHovered += UpdateHover;

        SelectedHex = Hex;
        Show();
    }

    public void UpdateHover(HexagonVisualization Hex)
    {
        if (SelectedHex == null)
        {
            Hide();
            return;
        }

        // don't update the selected Hex, just display hover effects
        Show();
    }

    private void Show()
    {
        if (!Game.TryGetService(out Units Units))
            return;

        if (!Units.TryGetUnitAt(SelectedHex.Location, out TokenizedUnitData Unit))
            return;

        SelectedUnit = Unit;
        ShowFeatures();
    }

    public void Hide()
    {
        Selector.OnItemHovered -= UpdateHover;

        SelectedUnit = null;
        HideFeatures();
    }

    public override UnitData GetFeatureObject()
    {
        return SelectedUnit;
    }
}
