using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SelectedHexScreen : ScreenFeatureGroup<HexagonData>
{
    public void Start() {
        Selector = GameObject.Find("Selector").GetComponent<Selector>().HexagonSelector;
        Selector.OnItemSelected += Show;
        Selector.OnItemDeSelected += Hide;

        Background = transform.GetChild(0).gameObject;
        Init();
        Hide();

        Game.Instance._OnPause += Hide;

    }

    public void Show(HexagonVisualization Hex) {
        SelectedHexTile = Hex != null ? Hex.Data : null;
        Show();
    }

    public void Show() {
        bool bShouldShow = SelectedHexTile != null;
        ShowBackground(bShouldShow);
        ShowWorker(bShouldShow);
        ShowFeatures();
    }

    public void Hide() {
        SelectedHexTile = null;
        ShowBackground(false);
        ShowWorker(false);
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

    private void ShowBackground(bool bShouldShow) {
        Background.SetActive(bShouldShow);
    }

   

    private void ShowWorker(bool bShouldShow) {
        if (!Game.TryGetService(out MapGenerator MapGenerator)) {
            bShouldShow = false;
        }

        BuildingData BuildingData = null;
        if (!MapGenerator || SelectedHexTile == null || !MapGenerator.TryGetBuildingAt(SelectedHexTile.Location, out BuildingData)) {
            bShouldShow = false;
        }

        WorkerContainerUI.SetActive(bShouldShow);

        if (!bShouldShow)
            return;

        foreach (Transform Child in WorkerContainerUI.transform) {
            Destroy(Child.gameObject);
        }

        for (int i = 0; i < BuildingData.MaxWorker; i++) {
            bool bShouldShowEmployee = BuildingData.AssignedWorkers[i] != null;
            GameObject Prefab = bShouldShowEmployee ? WorkerPrefab : NoWorkerPrefab;
            GameObject NewUI = Instantiate(Prefab);
            NewUI.transform.SetParent(WorkerContainerUI.transform, false);
            NewUI.transform.localPosition = OffsetUIWorker + OffsetUIPerWorker * i;

            Button Button = NewUI.GetComponent<Button>();
            if (Button) {
                int ti = i;
                if (!bShouldShowEmployee) {
                    Button.onClick.AddListener(() => { AddWorker(ti); });
                } else {
                    Button.onClick.AddListener(() => { RemoveWorker(ti); });
                }
            }
        }
    }

    public override HexagonData GetFeatureObject()
    {
        return SelectedHexTile;
    }

    public Selector<HexagonVisualization> Selector;
    public GameObject WorkerPrefab, NoWorkerPrefab, WorkerContainerUI;
    public GameObject UnitPrefab;
   
    private GameObject Background;
    private HexagonData SelectedHexTile;

    private static Vector3 OffsetUIWorker = new Vector3(-100, 0, 0);
    private static Vector3 OffsetUIPerWorker = new Vector3(75, 0, 0);

}
