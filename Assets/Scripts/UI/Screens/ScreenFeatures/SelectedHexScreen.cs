using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SelectedHexScreen : ScreenFeatureGroup<HexagonData>
{
    public void Start() {
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

    public void Show(HexagonVisualization Hex) {
        SelectedHexTile = Hex != null ? Hex.Data : null;
        Show();
    }

    public void Show() {
        ShowFeatures();
    }

    public void Hide() {
        SelectedHexTile = null;
        HideFeatures();
    }

    public void AddWorker(int i) {
        if (!Game.TryGetServices(out BuildingService Buildings, out Selectors Selector))
            return;

        if (!Buildings || SelectedHexTile == null || !Buildings.TryGetBuildingAt(SelectedHexTile.Location, out BuildingData Building))
            return;

        Selector.DeselectUI();

        Building.RequestAddWorkerAt(i);
        Show();
    }

    public void RemoveWorker(int i)
    {
        if (!Game.TryGetServices(out BuildingService Buildings, out Selectors Selector))
            return;

        if (SelectedHexTile == null || !Buildings.TryGetBuildingAt(SelectedHexTile.Location, out BuildingData Building))
            return;

        Selector.DeselectUI();

        Building.RequestRemoveWorkerAt(i);
        Show();
    }
       
    public override HexagonData GetFeatureObject()
    {
        return SelectedHexTile;
    }

    public override bool HasFeatureObject()
    {
        return SelectedHexTile != null;
    }

    public Selector<HexagonVisualization> Selector;
    public GameObject UnitPrefab;

    private HexagonData SelectedHexTile;


}
