using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_SelectedHex : MonoBehaviour
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
        SelectedHex = Hex != null ? Hex.Data : null;
        Show();
    }

    public void Show() {
        bool bShouldShow = SelectedHex != null;
        ShowBackground(bShouldShow);
        ShowTile(bShouldShow);
        ShowBuildingType(bShouldShow);
        ShowProduction(bShouldShow);
        ShowCorruption(bShouldShow);
        ShowWorker(bShouldShow);
    }

    public void Hide() {
        SelectedHex = null;
        ShowBackground(false);
        ShowTile(false);
        ShowBuildingType(false);
        ShowProduction(false);
        ShowCorruption(false);
        ShowWorker(false);
    }

    public void AddWorker(int Target) {
        if (!MapGenerator.TryGetBuildingAt(SelectedHex.Location, out BuildingData Building))
            return;

        Worker NewWorker = Workers.GetWorker();
        if (NewWorker == null)
            return;

        Building.AddWorker(NewWorker);
        Show();
    }

    public void RemoveWorker(int Target) {
        if (!MapGenerator.TryGetBuildingAt(SelectedHex.Location, out BuildingData Building))
            return;

        Worker ReturnedWorker = Building.RemoveWorkerAt(Target);
        Workers.ReturnWorker(ReturnedWorker, Building);
        Show();
    }

    private void ShowBackground(bool bShouldShow) {
        Background.SetActive(bShouldShow);
    }

    private void ShowTile(bool bShouldShow) {
        if (bShouldShow) {
            TileText.text = SelectedHex.Type.ToString();
        } else {
            TileText.text = string.Empty;
        }
    }

    private void ShowBuildingType(bool bShouldShow) {
        BuildingData BuildingData = null;
        if (bShouldShow && !MapGenerator.TryGetBuildingAt(SelectedHex.Location, out BuildingData)) {
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
        if (bShouldShow && !MapGenerator.TryGetBuildingAt(SelectedHex.Location, out BuildingData)) {
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
        if (bShouldShow && SelectedHex.bIsMalaised) {
            CorruptedText.text = "Corrupted";
        } else {
            CorruptedText.text = string.Empty;
        }
    }

    private void ShowWorker(bool bShouldShow) {
        BuildingData BuildingData = null;
        if (bShouldShow && !MapGenerator.TryGetBuildingAt(SelectedHex.Location, out BuildingData)) {
            bShouldShow = false;
        }

        WorkerContainerUI.SetActive(bShouldShow);

        if (!bShouldShow)
            return;

        foreach (Transform Child in WorkerContainerUI.transform) {
            Destroy(Child.gameObject);
        }

        for (int i = 0; i < BuildingData.GetMaxWorker(); i++) {
            Worker Worker = BuildingData.Workers.Count > i ? BuildingData.Workers[i] : null;
            GameObject Prefab = Worker == null ? NoWorkerUI : WorkerUI;
            GameObject NewUI = Instantiate(Prefab);
            NewUI.transform.SetParent(WorkerContainerUI.transform, false);
            NewUI.transform.localPosition = WorkerUIOffset + WorkerUISize * i;

            Button Button = NewUI.GetComponent<Button>();
            if (Button) {
                int ti = i;
                if (Worker == null) {
                    Button.onClick.AddListener(() => { AddWorker(ti); });
                } else {
                    Button.onClick.AddListener(() => { RemoveWorker(ti); });
                }
            }
            i++;
        }
    }

    public Selector<HexagonVisualization> Selector;
    public GameObject WorkerUI, NoWorkerUI, WorkerContainerUI;
   
    private TextMeshProUGUI TileText, BuildingTypeText, ProductionText, CorruptedText;
    private GameObject Background;
    private HexagonData SelectedHex;

    private static Vector3 WorkerUIOffset = new Vector3(-100, 90, 0);
    private static Vector3 WorkerUISize = new Vector3(75, 75, 0);
}
