using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SelectedHex : MonoBehaviour
{
    public void Start() {
        Selector = GameObject.Find("Selector").GetComponent<Selector>().HexagonSelector;
        Selector.OnItemSelected += Show;
        Selector.OnItemDeSelected += Hide;

        Background = transform.GetChild(0).gameObject;
        TileText = transform.GetChild(1).gameObject.GetComponent<TextMeshProUGUI>();
        BuildingTypeText = transform.GetChild(2).gameObject.GetComponent<TextMeshProUGUI>();
        ProductionText = transform.GetChild(3).gameObject.GetComponent<TextMeshProUGUI>();
        CorruptedText = transform.GetChild(4).gameObject.GetComponent<TextMeshProUGUI>();
        Hide();
    }

    public void Show(HexagonVisualization Hex) {
        SelectedHexTile = Hex != null ? Hex.Data : null;
        Show();
    }

    public void Show() {
        bool bShouldShow = SelectedHexTile != null;
        ShowBackground(bShouldShow);
        ShowTile(bShouldShow);
        ShowBuildingType(bShouldShow);
        ShowProduction(bShouldShow);
        ShowCorruption(bShouldShow);
        ShowWorker(bShouldShow);
        ShowUnits(bShouldShow);
    }

    public void Hide() {
        SelectedHexTile = null;
        ShowBackground(false);
        ShowTile(false);
        ShowBuildingType(false);
        ShowProduction(false);
        ShowCorruption(false);
        ShowWorker(false);
        ShowUnits(false);
    }

    public void AddWorker(int Target) {

        if (!MapGenerator.TryGetBuildingAt(SelectedHexTile.Location, out BuildingData Building))
            return;

        SelectWorkerScreen.OpenForBuilding(Building);
    }

    public void RemoveWorker(int Target) {
        if (!MapGenerator.TryGetBuildingAt(SelectedHexTile.Location, out BuildingData Building))
            return;

        WorkerData ReturnedWorker = Building.RemoveWorkerAt(Target);
        Workers.ReturnWorker(ReturnedWorker);
        Show();
        SelectWorkerScreen.InitWorkerDistances();
    }

    private void ShowBackground(bool bShouldShow) {
        Background.SetActive(bShouldShow);
    }

    private void ShowTile(bool bShouldShow) {
        if (bShouldShow) {
            TileText.text = SelectedHexTile.Type.ToString() + SelectedHexTile.Location.GlobalTileLocation.ToString();
        } else {
            TileText.text = string.Empty;
        }
    }

    private void ShowBuildingType(bool bShouldShow) {
        BuildingData BuildingData = null;
        if (bShouldShow && !MapGenerator.TryGetBuildingAt(SelectedHexTile.Location, out BuildingData)) {
            bShouldShow = false;
        }

        if (bShouldShow) {
            BuildingTypeText.text = BuildingData.GetBuildingType().ToString();
        } else {
            BuildingTypeText.text = string.Empty;
        }
    }

    private void ShowProduction(bool bShouldShow) {
        BuildingData BuildingData = null;
        if (bShouldShow && !MapGenerator.TryGetBuildingAt(SelectedHexTile.Location, out BuildingData)) {
            bShouldShow = false;
        }

        if (!bShouldShow) {
            ProductionText.text = string.Empty;
            return;
        }

        Production Production = BuildingData.GetProduction() + BuildingData.GetAdjacencyProduction();
        ProductionText.text = Production.GetShortDescription();
    }

    private void ShowCorruption(bool bShouldShow) {
        if (bShouldShow && SelectedHexTile.bIsMalaised) {
            CorruptedText.text = "Corrupted";
        } else {
            CorruptedText.text = string.Empty;
        }
    }

    private void ShowUnits(bool bShouldShow) { 
        foreach (Transform Child in UnitsUI.transform) {
            Destroy(Child.gameObject);
        }
        UnitsUI.SetActive(bShouldShow);
        if (!bShouldShow)
            return;

        Workers.TryGetWorkersAt(SelectedHexTile.Location, out List<WorkerData> WorkersAtHex);

        for (int i = 0; i < WorkersAtHex.Count; i++) {
            WorkerData Worker = WorkersAtHex[i];
            GameObject Prefab = Instantiate(UnitPrefab, UnitsUI.transform);
            Prefab.transform.localPosition = OffsetUIUnit + OffsetUIPerUnit * i;
            Prefab.transform.GetChild(1).GetComponent<TextMeshProUGUI>().text = Worker.Name;
        }
    }

    private void ShowWorker(bool bShouldShow) {
        BuildingData BuildingData = null;
        if (bShouldShow && !MapGenerator.TryGetBuildingAt(SelectedHexTile.Location, out BuildingData)) {
            bShouldShow = false;
        }

        WorkerContainerUI.SetActive(bShouldShow);

        if (!bShouldShow)
            return;

        foreach (Transform Child in WorkerContainerUI.transform) {
            Destroy(Child.gameObject);
        }

        for (int i = 0; i < BuildingData.GetMaxWorker(); i++) {
            WorkerData Worker = BuildingData.Workers.Count > i ? BuildingData.Workers[i] : null;
            GameObject Prefab = Worker == null ? NoWorkerPrefab : WorkerPrefab;
            GameObject NewUI = Instantiate(Prefab);
            NewUI.transform.SetParent(WorkerContainerUI.transform, false);
            NewUI.transform.localPosition = OffsetUIWorker + OffsetUIPerWorker * i;

            Button Button = NewUI.GetComponent<Button>();
            if (Button) {
                int ti = i;
                if (Worker == null) {
                    Button.onClick.AddListener(() => { AddWorker(ti); });
                } else {
                    Button.onClick.AddListener(() => { RemoveWorker(ti); });
                }
            }
        }
    }

    public Selector<HexagonVisualization> Selector;
    public GameObject WorkerPrefab, NoWorkerPrefab, WorkerContainerUI;
    public GameObject UnitPrefab, UnitsUI;

    public SelectWorkerScreen SelectWorkerScreen;
   
    private TextMeshProUGUI TileText, BuildingTypeText, ProductionText, CorruptedText;
    private GameObject Background;
    private HexagonData SelectedHexTile;

    private static Vector3 OffsetUIWorker = new Vector3(-100, 0, 0);
    private static Vector3 OffsetUIPerWorker = new Vector3(75, 0, 0);
    private static Vector3 OffsetUIUnit = new Vector3(0, 0, 0);
    private static Vector3 OffsetUIPerUnit = new Vector3(0, 55, 0);

}
