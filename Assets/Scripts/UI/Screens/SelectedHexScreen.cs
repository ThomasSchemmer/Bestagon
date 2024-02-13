using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SelectedHexScreen : MonoBehaviour
{
    public void Start() {
        Selector = GameObject.Find("Selector").GetComponent<Selector>().HexagonSelector;
        Selector.OnItemSelected += Show;
        Selector.OnItemDeSelected += Hide;

        Background = transform.GetChild(0).gameObject;
        LocationText = transform.GetChild(1).gameObject.GetComponent<TextMeshProUGUI>();
        TileText = transform.GetChild(2).gameObject.GetComponent<TextMeshProUGUI>();
        BuildingTypeText = transform.GetChild(3).gameObject.GetComponent<TextMeshProUGUI>();
        ProductionText = transform.GetChild(4).gameObject.GetComponent<TextMeshProUGUI>();
        CorruptedText = transform.GetChild(5).gameObject.GetComponent<TextMeshProUGUI>();
        Hide();

        MapGenerator = Game.GetService<MapGenerator>();

        Game.Instance._OnPause += Hide;
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

    public void RemoveWorker(int Target)
    {
        if (!Game.TryGetService(out Workers WorkerService))
            return;

        if (!MapGenerator.TryGetBuildingAt(SelectedHexTile.Location, out BuildingData Building))
            return;

        WorkerData ReturnedWorker = Building.RemoveWorkerAt(Target);
        WorkerService.ReturnWorker(ReturnedWorker);
        Show();
        SelectWorkerScreen.InitWorkerDistances();
    }

    private void ShowBackground(bool bShouldShow) {
        Background.SetActive(bShouldShow);
    }

    private void ShowTile(bool bShouldShow) {
        if (bShouldShow) {
            LocationText.text = SelectedHexTile.Location.GlobalTileLocation.ToString();
            TileText.text =
                SelectedHexTile.Type.ToString() + "\n" +
                SelectedHexTile.HexHeight +" "+ SelectedHexTile.Temperature + " "+SelectedHexTile.Humidity;
        } else {
            TileText.text = string.Empty;
            LocationText.text = string.Empty;
        }
    }

    private void ShowBuildingType(bool bShouldShow) {
        BuildingData BuildingData = null;
        if (bShouldShow && !MapGenerator.TryGetBuildingAt(SelectedHexTile.Location, out BuildingData)) {
            bShouldShow = false;
        }

        if (bShouldShow) {
            BuildingTypeText.text = BuildingData.BuildingType.ToString();
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

        Production Production = BuildingData.GetProduction();
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

        bool bHasBuilding = MapGenerator.TryGetBuildingAt(SelectedHexTile.Location, out BuildingData Building);
        UnitsUI.transform.localPosition = bHasBuilding ? OffsetUIUnit_OnBuilding : OffsetUIUnit;

        if (!Game.TryGetService(out Workers WorkerService))
            return;

        WorkerService.TryGetWorkersAt(SelectedHexTile.Location, out List<WorkerData> WorkersAtHex);

        for (int i = 0; i < WorkersAtHex.Count; i++) {
            WorkerData Worker = WorkersAtHex[i];
            GameObject Prefab = Instantiate(UnitPrefab, UnitsUI.transform);
            Prefab.transform.localPosition = OffsetUIPerUnit * i;
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

        for (int i = 0; i < BuildingData.MaxWorker; i++) {
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
   
    private TextMeshProUGUI LocationText, TileText, BuildingTypeText, ProductionText, CorruptedText;
    private GameObject Background;
    private HexagonData SelectedHexTile;
    private MapGenerator MapGenerator;

    private static Vector3 OffsetUIWorker = new Vector3(-100, 0, 0);
    private static Vector3 OffsetUIPerWorker = new Vector3(75, 0, 0);
    private static Vector3 OffsetUIUnit = new Vector3(0, 155, 0);
    private static Vector3 OffsetUIUnit_OnBuilding = new Vector3(0, 200, 0);
    private static Vector3 OffsetUIPerUnit = new Vector3(0, 55, 0);

}
