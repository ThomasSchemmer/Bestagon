using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StockpileScreen : MonoBehaviour
{
    public void Start()
    {
        Game.RunAfterServicesInit((Stockpile Stockpile, Turn Turn) =>
        {
            Game.RunAfterServicesInit((IconFactory IconFactory, Workers Workers) => 
            {
                // update visuals everytime something changes in the amounts
                Stockpile._OnResourcesChanged += UpdateVisuals;
                Workers._OnWorkersChanged += UpdateVisuals;
                // only update the +/- indicators every turn
                Turn._OnTurnEnd += UpdateIndicatorCount;

                Initialize(Stockpile, IconFactory);
                UpdateVisuals();
            });
        });

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

        GameObject WorkerVisuals = IconFactory.GetVisualsForMiscalleneous(IconFactory.MiscellaneousType.Worker, 0);
        WorkerScreen = WorkerVisuals.GetComponent<NumberedIconScreen>();
        RectTransform WorkerRect = WorkerVisuals.GetComponent<RectTransform>();
        WorkerRect.SetParent(transform, false);
        WorkerRect.anchoredPosition = new Vector2(
            (StockpileGroupScreen.WIDTH + StockpileGroupScreen.OFFSET) * (GroupCount + 1),
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

        if (!Game.TryGetService(out Workers Workers))
            return;

        WorkerScreen.UpdateVisuals(Workers.GetUnemployedWorkerCount(), Workers.GetTotalWorkerCount());
    }

    public GameObject GroupPrefab;
    public GameObject ItemPrefab;

    private StockpileGroupScreen[] GroupScreens;
    private NumberedIconScreen WorkerScreen;
}