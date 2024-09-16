using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WorkerScreenFeature : ScreenFeature<HexagonData>
{
    public GameObject WorkerPrefab, NoWorkerPrefab;

    public override bool ShouldBeDisplayed()
    {
        if (!TryGetBuilding(out BuildingEntity Building))
            return false;

        return true;
    }

    private bool TryGetBuilding(out BuildingEntity Building)
    {
        Building = null;
        HexagonData SelectedHex = Target.GetFeatureObject();
        if (SelectedHex == null)
            return false;

        if (!Game.TryGetService(out BuildingService Buildings))
            return false;

        if (!Buildings.TryGetEntityAt(SelectedHex.Location, out Building))
            return false;

        return true;
    }


    public override void ShowAt(float YOffset)
    {
        float Height = GetHeight();
        YOffset += Height / 2.0f;
        base.ShowAt(YOffset);

        DeleteWorkerVisuals();
        SetConditionalPadding(Height);

        TryGetBuilding(out BuildingEntity BuildingData);

        for (int i = 0; i < BuildingData.MaxWorker; i++)
        {
            CreateWorkerUI(BuildingData, i);
        }
    }

    private void CreateWorkerUI(BuildingEntity BuildingData, int i)
    {
        SelectedHexScreen HexScreen = (SelectedHexScreen)Target;
        bool bShouldShowEmployee = BuildingData.AssignedWorkers[i] != null;
        GameObject Prefab = bShouldShowEmployee ? WorkerPrefab : NoWorkerPrefab;
        GameObject WorkerUI = Instantiate(Prefab);
        RectTransform WorkerTransform = WorkerUI.GetComponent<RectTransform>();
        WorkerTransform.SetParent(transform, false);
        WorkerTransform.anchoredPosition = OffsetUI + OffsetUIPerWorker * i;

        Button Button = WorkerUI.GetComponent<Button>();
        if (!Button)
            return;

        int ti = i;
        if (!bShouldShowEmployee)
        {
            Button.onClick.AddListener(() => { HexScreen.AddWorker(ti); });
        }
        else
        {
            Button.onClick.AddListener(() => { HexScreen.RemoveWorker(ti); });
        }
    }

    public override void Hide()
    {
        base.Hide();
        DeleteWorkerVisuals();
        SetConditionalPadding(0);
    }

    private void DeleteWorkerVisuals()
    {
        foreach (Transform Child in transform)
        {
            Destroy(Child.gameObject);
        }
    }

    private static Vector3 OffsetUIPerWorker = new Vector3(75, 0, 0);
    private static Vector3 OffsetUI = OffsetUIPerWorker / 2.0f + new Vector3(10, 0, 0);
}
