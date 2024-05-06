using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectedUnitsScreen : ScreenFeatureGroup<UnitData>
{
    public Selector<HexagonVisualization> Selector;

    private UnitData SelectedUnit;

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
        if (!Game.TryGetService(out Units Units))
            return;

        if (!Units.TryGetUnitAt(Hex.Location, out TokenizedUnitData Unit))
            return;

        SelectedUnit = Unit;
        ShowFeatures();
    }

    public void Hide()
    {
        SelectedUnit = null;
        HideFeatures();
    }

    public override UnitData GetFeatureObject()
    {
        return SelectedUnit;
    }
}
