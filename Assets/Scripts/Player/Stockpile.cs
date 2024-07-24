using System;
using System.Collections.Generic;
using Unity.Collections;

public class Stockpile : GameService, ISaveableService, IQuestRegister<Production>
{

    public bool Pay(Production Costs) {
        if (!CanAfford(Costs))
            return false;

        Resources -= Costs;
        _OnResourcesChanged?.Invoke();
        return true;
    }

    public void ProduceWorkers()
    {
        if (!Game.TryGetServices(out BuildingService BuildingService, out Workers WorkerService))
            return;

        List<BuildingData> Buildings = BuildingService.Buildings;

        foreach (BuildingData Building in Buildings)
        {
            if (Building.Effect.EffectType != OnTurnBuildingEffect.Type.ProduceUnit)
                continue;

            if (Building.GetWorkingWorkerCount() != 2)
                continue;

            Building.RequestRemoveWorkerAt(0);
            Building.RequestRemoveWorkerAt(1);
            WorkerService.CreateNewWorker();
        }
    }
    
    public Production CalculateResources(bool bIsSimulated)
    {
        if (!Game.TryGetService(out MapGenerator MapGenerator))
            return Production.Empty;

        Production ProducedThisRound = MapGenerator.GetProductionPerTurn();

        if (bIsSimulated)
        {
            return SimulateStarvation(ProducedThisRound);
        }
        else
        {
            return CalculateStarvation(ProducedThisRound);
        }
    }

    private Production CalculateStarvation(Production ProducedThisRound)
    {
        if (!Game.TryGetServices(out Workers WorkerService, out Units UnitService))
            return Production.Empty;

        Resources += ProducedThisRound;
        Production PreStarvation = Resources.Copy();
        HandleStarvation(WorkerService, UnitService, Resources, false);
        Production AfterStarvation = Resources.Copy();

        return AfterStarvation - PreStarvation;
    }

    private Production SimulateStarvation(Production ProducedThisRound)
    {
        if (!Game.TryGetServices(out Workers WorkerService, out Units UnitService))
            return Production.Empty;

        SimulatedResources = Resources.Copy();
        Production PreProduction = SimulatedResources.Copy();
        SimulatedResources += ProducedThisRound;
        HandleStarvation(WorkerService, UnitService, SimulatedResources, true);
        Production AfterStarvation = SimulatedResources.Copy();

        SimulatedGains = AfterStarvation - PreProduction;
        _OnSimulatedGainsChanged?.Invoke();

        return SimulatedGains;
    }

    public void GenerateResources()
    {
        Production ProducedThisRound = CalculateResources(false);
        _OnResourcesCollected.ForEach(_ => _.Invoke(ProducedThisRound));
        _OnResourcesChanged?.Invoke();
    }

    public void AddResources(Production Production)
    {
        Resources += Production;
        _OnResourcesChanged?.Invoke();
    }

    public int GetResourceGroupCount(int GroupIndex, bool bIsSimulated) 
    {
        int Count = 0;
        int Min = Production.Indices[GroupIndex];
        int Max = Production.Indices[GroupIndex + 1];
        Production Target = bIsSimulated ? SimulatedGains : Resources;
        for (int Index = Min; Index < Max; Index++)
        {
            Production.Type ResourceType = (Production.Type)Index;
            Count += Target[ResourceType];
        }
        return Count;
    }

    public int GetResourceCount(int ResourceTypeIndex, bool bIsSimulated)
    {
        Production Target = bIsSimulated ? SimulatedGains : Resources;
        Production.Type ResourceType = (Production.Type)ResourceTypeIndex;
        return Target[ResourceType];
    }

    private void HandleStarvation(Workers WorkerService, Units UnitService, Production TargetResources, bool bIsSimulated)
    {
        StarvableUnitData.HandleStarvationFor(WorkerService.Units, TargetResources, "Workers", bIsSimulated);
        StarvableUnitData.HandleStarvationFor(UnitService.Units, TargetResources, "Units", bIsSimulated);
    }

    public bool CanAfford(Production Costs) {
        return Costs <= Resources;
    }

    public void RequestUIRefresh()
    {
        _OnResourcesChanged?.Invoke();
    }

    private void OnSimulateResources()
    {
        CalculateResources(true);
    }

    protected override void StartServiceInternal()
    {
        Game.RunAfterServiceInit((SaveGameManager Manager) =>
        {
            Workers._OnWorkersChanged += OnSimulateResources;
            BuildingService._OnBuildingsChanged += OnSimulateResources;
            _OnResourcesChanged += OnSimulateResources; 

            if (Manager.HasDataFor(ISaveableService.SaveGameType.Stockpile))
                return;

            Refill();
            _OnInit?.Invoke(this);
        });
    }

    protected override void StopServiceInternal() {}

    public void OnDestroy()
    {
        Workers._OnWorkersChanged -= OnSimulateResources;
        BuildingService._OnBuildingsChanged -= OnSimulateResources;
        _OnResourcesChanged -= OnSimulateResources;
    }

    public int GetSize()
    {
        // flag for refreshing resources, upgrade points
        return Resources.GetSize() + sizeof(int) + sizeof(byte); 
    }

    public byte[] GetData()
    {
        bShouldReset = Game.IsIn(Game.GameState.CardSelection);

        NativeArray<byte> Bytes = new(GetSize(), Allocator.Temp);
        int Pos = 0;

        Pos = SaveGameManager.AddSaveable(Bytes, Pos, Resources);
        Pos = SaveGameManager.AddInt(Bytes, Pos, UpgradePoints);
        Pos = SaveGameManager.AddBool(Bytes, Pos, bShouldReset);

        return Bytes.ToArray();
    }

    public void SetData(NativeArray<byte> Bytes)
    {
        int Pos = 0;
        Resources = new Production();
        Pos = SaveGameManager.SetSaveable(Bytes, Pos, Resources);
        Pos = SaveGameManager.GetInt(Bytes, Pos, out UpgradePoints);
        Pos = SaveGameManager.GetBool(Bytes, Pos, out bShouldReset);
    }

    public void Load()
    {
        Refill();
        _OnInit?.Invoke(this);
    }

    public void Refill()
    {
        // does not reset upgrade points nor coins!
        int CoinCount = Resources[Production.Type.Coins];
        Reset();
        AddResources(StartingResources);
        AddResources(new Production(Production.Type.Coins, CoinCount));
    }

    public void Reset()
    {
        Resources = new();
    }

    public Production Resources;
    public Production SimulatedResources;
    public Production SimulatedGains;

    public Production StartingResources;
    public int UpgradePoints = 0;

    protected bool bShouldReset = false;

    public delegate void OnResourcesChanged();
    public static OnResourcesChanged _OnResourcesChanged;
    public static OnResourcesChanged _OnSimulatedGainsChanged;

    public static ActionList<Production> _OnResourcesCollected = new();
}
