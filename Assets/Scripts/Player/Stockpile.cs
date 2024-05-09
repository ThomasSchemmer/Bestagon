using System.Collections.Generic;
using Unity.Collections;

public class Stockpile : GameService, ISaveable
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
        if (!Game.TryGetServices(out MapGenerator MapGenerator, out Workers WorkerService))
            return;

        if (!MapGenerator.TryGetAllBuildings(out List<BuildingData> Buildings))
            return;

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
    
    public void ProduceResources() {
        if (!Game.TryGetService(out MapGenerator MapGenerator))
            return;

        if (!Game.TryGetServices(out Workers WorkerService, out Units UnitService))
            return;

        Resources += MapGenerator.GetProductionPerTurn();
        HandleStarvation(WorkerService, UnitService);

        _OnResourcesChanged?.Invoke();
    }

    public void AddResources(Production Production)
    {
        Resources += Production;
        _OnResourcesChanged?.Invoke();
    }

    public int GetResourceGroupCount(int GroupIndex) 
    {
        int Count = 0;
        int Min = Production.Indices[GroupIndex];
        int Max = Production.Indices[GroupIndex + 1];
        for (int Index = Min; Index < Max; Index++)
        {
            Production.Type ResourceType = (Production.Type)Index;
            Count += Resources[ResourceType];
        }
        return Count;
    }

    public int GetResourceCount(int ResourceTypeIndex)
    {
        Production.Type ResourceType = (Production.Type)ResourceTypeIndex;
        return Resources[ResourceType];
    }

    private void HandleStarvation(Workers WorkerService, Units UnitService)
    {
        StarvableUnitData.HandleStarvationFor(WorkerService.ActiveWorkers, Resources, "Workers");
        StarvableUnitData.HandleStarvationFor(UnitService.ActiveUnits, Resources, "Units");
    }

    public bool CanAfford(Production Costs) {
        return Costs <= Resources;
    }

    public void RequestUIRefresh()
    {
        _OnResourcesChanged?.Invoke();
    }

    protected override void StartServiceInternal()
    {
        Resources = new(new[]
        {
            Production.Type.Wood,
            Production.Type.Clay,
            Production.Type.Mushroom,
        }, new[]
        {
            5,
            2,
            5
        });
        _OnInit?.Invoke();
    }

    protected override void StopServiceInternal() {}

    public int GetSize()
    {
        return Resources.GetSize() + sizeof(int); 
    }

    public byte[] GetData()
    {
        NativeArray<byte> Bytes = new(GetSize(), Allocator.Temp);
        int Pos = 0;

        Pos = SaveGameManager.AddSaveable(Bytes, Pos, Resources);
        Pos = SaveGameManager.AddInt(Bytes, Pos, UpgradePoints);

        return Bytes.ToArray();
    }

    public void SetData(NativeArray<byte> Bytes)
    {
        int Pos = 0;
        Resources = new Production();
        Pos = SaveGameManager.SetSaveable(Bytes, Pos, Resources);
        Pos = SaveGameManager.GetInt(Bytes, Pos, out UpgradePoints);
    }

    public Production Resources;
    public int UpgradePoints = 0;

    public delegate void OnResourcesChanged();
    public OnResourcesChanged _OnResourcesChanged;
}
