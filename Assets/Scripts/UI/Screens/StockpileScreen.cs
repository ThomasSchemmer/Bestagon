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
            Stockpile._OnUpgradesChanged += UpdateUpgradesVisuals;
            Workers._OnWorkersChanged += UpdateWorkerVisuals;
            Units._OnUnitCountChanged += UpdateScoutVisuals;
            TokenizedUnitData._OnMovement += UpdateScoutVisuals;
            Turn._OnTurnEnd += UpdateIndicatorCount;
            Stockpile._OnSimulatedGainsChanged += UpdateIndicatorCount;

            Initialize(Stockpile, IconFactory);
            UpdateVisuals();
            Show(bShow);
        });

    }

    private void OnDestroy()
    {
        Stockpile._OnResourcesChanged -= UpdateVisuals;
        Stockpile._OnUpgradesChanged -= UpdateUpgradesVisuals;
        Workers._OnWorkersChanged -= UpdateWorkerVisuals;
        Units._OnUnitCountChanged -= UpdateScoutVisuals;
        TokenizedUnitData._OnMovement -= UpdateScoutVisuals;
        Turn._OnTurnEnd -= UpdateIndicatorCount;
        Stockpile._OnSimulatedGainsChanged -= UpdateIndicatorCount;
    }

    protected virtual void Initialize(Stockpile Stockpile, IconFactory IconFactory)
    {
        InitializeGroupScreens(Stockpile, IconFactory);
        InitializeWorkerVisuals(IconFactory);
        InitializeScoutVisuals(IconFactory);
        InitializeUpgradesVisuals(IconFactory);
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

    private void InitializeUpgradesVisuals(IconFactory IconFactory)
    {
        if (!ShouldDisplayUpgrades())
            return;

        int GroupCount = Production.Indices.Length - 1;
        GameObject UpgradesVisuals = IconFactory.GetVisualsForMiscalleneous(IconFactory.MiscellaneousType.Upgrades, null, 0);
        this.UpgradesScreen = UpgradesVisuals.GetComponent<NumberedIconScreen>();
        this.UpgradesScreen.HoverTooltip = "Upgrade points";
        RectTransform UpgradesScreen = UpgradesVisuals.GetComponent<RectTransform>();
        UpgradesScreen.SetParent(GetTargetTransform(), false);
        UpgradesScreen.anchoredPosition = GetTargetOrigin();
        UpgradesScreen.anchoredPosition += GetTargetOffset(GroupCount + 3);
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
        UpdateUpgradesVisuals();
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
                (StockpileGroupScreen.WIDTH + StockpileGroupScreen.OFFSET) * i + StockpileGroupScreen.INITIAL_OFFSET,
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

    protected virtual bool ShouldDisplayUpgrades()
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

    private void UpdateUpgradesVisuals()
    {
        if (!ShouldDisplayUpgrades())
            return;

        if (!Game.TryGetService(out Stockpile Stockpile))
            return;

        UpgradesScreen.UpdateVisuals(Stockpile.UpgradePoints);
    }

    public void Show(bool bShow)
    {
        this.bShow = bShow;
        if (GroupScreens == null)
            return;

        foreach (StockpileGroupScreen GroupScreen in GroupScreens)
        {
            GroupScreen.Show(bShow);
        }
        if (ShouldDisplayWorkers())
        {
            WorkerScreen.Show(bShow);
        }
        if (ShouldDisplayScouts())
        {
            ScoutScreen.Show(bShow);    
        }
        if (ShouldDisplayUpgrades())
        {
            UpgradesScreen.Show(bShow);
        }
    }

    public GameObject GroupPrefab;
    public GameObject ItemPrefab;

    protected StockpileGroupScreen[] GroupScreens;
    protected NumberedIconScreen WorkerScreen;
    protected NumberedIconScreen ScoutScreen;
    protected NumberedIconScreen UpgradesScreen;
    protected bool bShow = true;
}
