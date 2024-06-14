using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StockpileScreen : MonoBehaviour
{
    public bool bDisplayActiveOnly = false;

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

    protected virtual void Initialize(Stockpile Stockpile, IconFactory IconFactory)
    {
        InitializeGroupScreens(Stockpile, IconFactory);
        InitializeWorkerVisuals(IconFactory);
        InitializeScoutVisuals(IconFactory);
    }

    private void InitializeGroupScreens(Stockpile Stockpile, IconFactory IconFactory)
    {
        int GroupCount = Production.Indices.Length - 1;
        GroupScreens = new StockpileGroupScreen[GroupCount];
        for (int i = 0; i < GroupCount; i++)
        {
            GameObject GroupObject = Instantiate(GroupPrefab);
            StockpileGroupScreen Screen = GroupObject.GetComponent<StockpileGroupScreen>();
            Screen.Initialize(this, i, ItemPrefab, Stockpile, IconFactory, bDisplayActiveOnly);
            Screen.transform.SetParent(GetTargetTransform(), false);
            RectTransform ScreenRect = Screen.GetComponent<RectTransform>();
            ScreenRect.anchoredPosition = GetTargetOrigin();
            ScreenRect.anchoredPosition += GetTargetOffset(i);
            GroupScreens[i] = Screen;
        }
    }

    private void InitializeWorkerVisuals(IconFactory IconFactory)
    {
        if (!ShouldDisplayWorkers())
            return;

        int GroupCount = Production.Indices.Length - 1;
        GameObject WorkerVisuals = IconFactory.GetVisualsForMiscalleneous(IconFactory.MiscellaneousType.Worker, null, 0);
        WorkerScreen = WorkerVisuals.GetComponent<NumberedIconScreen>();
        WorkerScreen.HoverTooltip = "Unemployed / maximum worker count";
        RectTransform WorkerRect = WorkerVisuals.GetComponent<RectTransform>();
        WorkerRect.SetParent(GetTargetTransform(), false);
        WorkerRect.anchoredPosition = GetTargetOrigin();
        WorkerRect.anchoredPosition += GetTargetOffset(GroupCount + 1);
    }

    private void InitializeScoutVisuals(IconFactory IconFactory)
    {
        if (!ShouldDisplayScouts())
            return;

        int GroupCount = Production.Indices.Length - 1;
        GameObject ScoutVisuals = IconFactory.GetVisualsForMiscalleneous(IconFactory.MiscellaneousType.Scout, null, 0);
        ScoutScreen = ScoutVisuals.GetComponent<NumberedIconScreen>();
        ScoutScreen.HoverTooltip = "Idle / maximum scouts";
        RectTransform ScoutRect = ScoutVisuals.GetComponent<RectTransform>();
        ScoutRect.SetParent(GetTargetTransform(), false);
        ScoutRect.anchoredPosition = GetTargetOrigin();
        ScoutRect.anchoredPosition += GetTargetOffset(GroupCount + 2);
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

    protected virtual Transform GetTargetTransform()
    {
        return transform;
    }

    protected virtual Vector2 GetTargetOrigin()
    {
        return new Vector2(StockpileGroupScreen.WIDTH / 2f, 0);
    }

    protected virtual Vector2 GetTargetOffset(int i)
    {
        return new Vector2(
                (StockpileGroupScreen.WIDTH + StockpileGroupScreen.OFFSET) * i,
                0
            );
    }

    public virtual Vector2 GetContainerSize()
    {
        // y value will be dynamic with element count
        return new(175, 180);
    }

    public virtual Vector2 GetElementSize()
    {
        return new Vector2(72, 30);
    }

    public virtual Vector2 GetElementOffset()
    {
        return new Vector2(0, 10);
    }

    public Vector2 GetElementTotalSize()
    {
        return GetElementSize() + GetElementOffset();
    }

    public Vector2 GetContainerOffset() {
        return new(GetContainerSize().x / 3, 25);
    }

    public virtual bool ShouldHeaderBeIcon()
    {
        return true;
    }

    protected virtual bool ShouldDisplayScouts()
    {
        return true;
    }

    protected virtual bool ShouldDisplayWorkers()
    {
        return true;
    }

    private void UpdateWorkerVisuals()
    {
        if (!ShouldDisplayWorkers())
            return;

        if (!Game.TryGetService(out Workers Workers))
            return;

        WorkerScreen.UpdateVisuals(Workers.GetUnemployedWorkerCount(), Workers.GetTotalWorkerCount());

    }

    public virtual void AdaptItemScreen(StockpileGroupScreen GroupScreen, StockpileItemScreen ItemScreen) {}

    // just for callback from event
    private void UpdateScoutVisuals(int i)
    {
        UpdateScoutVisuals();
    }

    private void UpdateScoutVisuals()
    {
        if (!ShouldDisplayScouts())
            return;

        if (!Game.TryGetService(out Units Units))
            return;

        ScoutScreen.UpdateVisuals(Units.GetIdleScoutCount(), Units.GetMaxScoutCount());
    }

    public GameObject GroupPrefab;
    public GameObject ItemPrefab;

    protected StockpileGroupScreen[] GroupScreens;
    protected NumberedIconScreen WorkerScreen;
    protected NumberedIconScreen ScoutScreen;
}
