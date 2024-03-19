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
    }

    public void Hide() {
        SelectedHexTile = null;
        ShowBackground(false);
        ShowTile(false);
        ShowBuildingType(false);
        ShowProduction(false);
        ShowCorruption(false);
        ShowWorker(false);
    }


    public void AddWorker() {
        if (!MapGenerator.TryGetBuildingAt(SelectedHexTile.Location, out BuildingData Building))
            return;

        Building.AddWorker();
        ShowWorker(true);
    }

    public void RemoveWorker()
    {
        if (!MapGenerator.TryGetBuildingAt(SelectedHexTile.Location, out BuildingData Building))
            return;

        Building.RemoveWorker();
        ShowWorker(true);
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
            bool bShouldShowEmployee = i < BuildingData.WorkerCount;
            GameObject Prefab = bShouldShowEmployee ? WorkerPrefab : NoWorkerPrefab;
            GameObject NewUI = Instantiate(Prefab);
            NewUI.transform.SetParent(WorkerContainerUI.transform, false);
            NewUI.transform.localPosition = OffsetUIWorker + OffsetUIPerWorker * i;

            Button Button = NewUI.GetComponent<Button>();
            if (Button) {
                int ti = i;
                if (!bShouldShowEmployee) {
                    Button.onClick.AddListener(() => { AddWorker(); });
                } else {
                    Button.onClick.AddListener(() => { RemoveWorker(); });
                }
            }
        }
    }

    public Selector<HexagonVisualization> Selector;
    public GameObject WorkerPrefab, NoWorkerPrefab, WorkerContainerUI;
    public GameObject UnitPrefab;
   
    private TextMeshProUGUI LocationText, TileText, BuildingTypeText, ProductionText, CorruptedText;
    private GameObject Background;
    private HexagonData SelectedHexTile;
    private MapGenerator MapGenerator;

    private static Vector3 OffsetUIWorker = new Vector3(-100, 0, 0);
    private static Vector3 OffsetUIPerWorker = new Vector3(75, 0, 0);

}
