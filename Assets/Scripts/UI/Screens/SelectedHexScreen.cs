﻿using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SelectedHexScreen : ScreenFeatureGroup<HexagonData>
{
    public void Start() {
        Selector = GameObject.Find("Selector").GetComponent<Selector>().HexagonSelector;
        Selector.OnItemSelected += Show;
        Selector.OnItemDeSelected += Hide;

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
        if (!Game.TryGetService(out MapGenerator MapGenerator))
            return;

        if (!MapGenerator || SelectedHexTile == null || !MapGenerator.TryGetBuildingAt(SelectedHexTile.Location, out BuildingData Building))
            return;

        Building.RequestAddWorkerAt(i);
        Show();
    }

    public void RemoveWorker(int i)
    {
        if (!Game.TryGetService(out MapGenerator MapGenerator))
            return;

        if (SelectedHexTile == null || !MapGenerator.TryGetBuildingAt(SelectedHexTile.Location, out BuildingData Building))
            return;

        Building.RequestRemoveWorkerAt(i);
        Show();
    }
       
    public override HexagonData GetFeatureObject()
    {
        return SelectedHexTile;
    }

    public Selector<HexagonVisualization> Selector;
    public GameObject UnitPrefab;

    private HexagonData SelectedHexTile;


}