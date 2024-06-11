using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StockpileScreen : MonoBehaviour
{
    public void Start()
    {
        Game.RunAfterServicesInit((Stockpile Stockpile, IconFactory IconFactory) =>
        {
            // update visuals everytime something changes in the amounts
            Stockpile._OnResourcesChanged += UpdateVisuals;
            Workers._OnWorkersChanged += UpdateWorkerVisuals;
            Units._OnUnitCountChanged += UpdateScoutVisuals;
            TokenizedUnitData._OnMovement += UpdateScoutVisuals;
            // only update the +/- indicators every turn
            Turn._OnTurnEnd += UpdateIndicatorCount;

            Initialize(Stockpile, IconFactory);
            UpdateVisuals();
        });

    }

    private void OnDestroy()
    {
        Stockpile._OnResourcesChanged -= UpdateVisuals;
        Workers._OnWorkersChanged -= UpdateWorkerVisuals;
        Units._OnUnitCountChanged -= UpdateScoutVisuals;
        TokenizedUnitData._OnMovement -= UpdateScoutVisuals;
        Turn._OnTurnEnd -= UpdateIndicatorCount;
    }

    private void Initialize(Stockpile Stockpile, IconFactory IconFactory)
    {
        int GroupCount = Production.Indices.Length - 1;
        GroupScreens = new StockpileGroupScreen[GroupCount];
        for (int i = 0; i < GroupCount; i++)
        {
            GameObject GroupObject = Instantiate(GroupPrefab);
            StockpileGroupScreen Screen = GroupObject.GetComponent<StockpileGroupScreen>();
            Screen.Initialize(i, ItemPrefab, Stockpile, IconFactory);
            Screen.transform.SetParent(transform, false);
            Screen.transform.position = new Vector3(
                (StockpileGroupScreen.WIDTH + StockpileGroupScreen.OFFSET) * i,
                0,
                0)
                + Screen.transform.position;
            GroupScreens[i] = Screen;
        }

        GameObject WorkerVisuals = IconFactory.GetVisualsForMiscalleneous(IconFactory.MiscellaneousType.Worker, null, 0);
        WorkerScreen = WorkerVisuals.GetComponent<NumberedIconScreen>();
        WorkerScreen.HoverTooltip = "Unemployed / maximum worker count";
        RectTransform WorkerRect = WorkerVisuals.GetComponent<RectTransform>();
        WorkerRect.SetParent(transform, false);
        WorkerRect.anchoredPosition = new Vector2(
            (StockpileGroupScreen.WIDTH + StockpileGroupScreen.OFFSET) * (GroupCount + 1),
            0);


        GameObject ScoutVisuals = IconFactory.GetVisualsForMiscalleneous(IconFactory.MiscellaneousType.Scout, null, 0);
        ScoutScreen = ScoutVisuals.GetComponent<NumberedIconScreen>();
        ScoutScreen.HoverTooltip = "Idle / maximum scouts";
        RectTransform ScoutRect = ScoutVisuals.GetComponent<RectTransform>();
        ScoutRect.SetParent(transform, false);
        ScoutRect.anchoredPosition = new Vector2(
            (StockpileGroupScreen.WIDTH + StockpileGroupScreen.OFFSET) * (GroupCount + 2),
            0);
    }

    private void UpdateIndicatorCount()
    {
        foreach (StockpileGroupScreen GroupScreen in GroupScreens) {
            GroupScreen.UpdateIndicatorCount();
        }
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        foreach (StockpileGroupScreen Group in GroupScreens)
        {
            Group.UpdateVisuals();
        }

        UpdateWorkerVisuals();
        UpdateScoutVisuals();
    }

    private void UpdateWorkerVisuals()
    {
        if (!Game.TryGetService(out Workers Workers))
            return;

        WorkerScreen.UpdateVisuals(Workers.GetUnemployedWorkerCount(), Workers.GetTotalWorkerCount());

    }

    // just for callnback from event
    private void UpdateScoutVisuals(int i)
    {
        UpdateScoutVisuals();
    }

    private void UpdateScoutVisuals()
    {
        if (!Game.TryGetService(out Units Units))
            return;

        ScoutScreen.UpdateVisuals(Units.GetIdleScoutCount(), Units.GetMaxScoutCount());
    }

    public GameObject GroupPrefab;
    public GameObject ItemPrefab;

    private StockpileGroupScreen[] GroupScreens;
    private NumberedIconScreen WorkerScreen;
    private NumberedIconScreen ScoutScreen;
}
