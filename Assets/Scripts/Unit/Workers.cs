using System.Collections.Generic;
using UnityEngine;

public class Workers : EntityProvider<StarvableUnitEntity>
{
    public void RequestAddWorkerFor(BuildingEntity Building, int i) {
        if (!HasUnemployedWorkers())
        {
            MessageSystemScreen.CreateMessage(Message.Type.Error, "No idle worker exist for this building");
            return;
        }

        WorkerEntity WorkerUnit = GetUnemployedWorker();
        AssignWorkerTo(WorkerUnit, Building, i);
    }

    public void RequestRemoveWorkerFor(BuildingEntity Building, WorkerEntity Worker, int i)
    {
        Worker.RemoveFromBuilding();
        Building.RemoveWorker(i);
        _OnWorkersChanged?.Invoke();
        _OnWorkersAssigned?.Invoke(Building.GetLocations());
        _OnWorkerAssignedList.ForEach(_ => _.Invoke(Worker));
    }

    public void AssignWorkerTo(WorkerEntity Worker, BuildingEntity Building, int i)
    {
        Worker.AssignToBuilding(Building, i);
        Building.PutWorkerAt(Worker, i);
        _OnWorkersChanged?.Invoke();
        _OnWorkersAssigned?.Invoke(Building.GetLocations());
        _OnWorkerAssignedList.ForEach(_ => _.Invoke(Worker));
    }

    public void KillWorker(WorkerEntity WorkerUnit)
    {
        if (WorkerUnit == null)
            return;

        if (WorkerUnit.IsEmployed())
        {
            RequestRemoveWorkerFor(WorkerUnit.GetAssignedBuilding(), WorkerUnit, WorkerUnit.GetAssignedBuildingSlot());
        }
        Entities.Remove(WorkerUnit);
        _OnWorkersChanged?.Invoke();
        CheckForGameOver();
    }

    public void KillWorkers(int Count)
    {
        for (int i = 0; i < Count; i++)
        {
            KillRandomWorker(); 
        }
    }

    private void KillRandomWorker()
    {
        // take unemployed first to minimize player annoyance
        var Worker = GetUnemployedWorker();
        if (Worker == null)
        {
            Worker = (WorkerEntity)Entities[0];
        }
        KillWorker(Worker);
    }

    private void CheckForGameOver()
    {
        if (Entities.Count != 0)
            return;

        if (!Game.TryGetService(out Units UnitService))
            return;

        if (UnitService.Entities.Count != 0)
            return;

        Game.Instance.GameOver("Your tribe has died out!");
    }

    private WorkerEntity GetUnemployedWorker()
    {
        foreach (WorkerEntity WorkerUnit in Entities)
        {
            if (!WorkerUnit.IsEmployed())
                return WorkerUnit;
        }
        return null;
    }

    public int GetEmployedWorkerCount() {
        int EmployedCount = 0;
        foreach (WorkerEntity WorkerUnit in Entities)
        {
            if (!WorkerUnit.IsEmployed())
                continue;

            EmployedCount++;
        }

        return EmployedCount;
    }

    public int GetUnemployedWorkerCount() {
        return GetTotalWorkerCount() - GetEmployedWorkerCount();
    }

    public bool HasUnemployedWorkers()
    {
        return GetUnemployedWorkerCount() > 0;
    }

    public int GetTotalWorkerCount() {
        return Entities.Count;
    }

    public override bool TryCreateNewEntity(int EntityCode, LocationSet Location)
    {
        if ((UnitEntity.UType)EntityCode != UnitEntity.UType.Worker)
            return false;

        if (!Game.TryGetService(out MeshFactory MeshFactory))
            return false;

        WorkerEntity Worker = (WorkerEntity)MeshFactory.CreateDataFromType(UnitEntity.UType.Worker);
        AddWorker(Worker);
        return true;
    }

    public void AddWorker(WorkerEntity Worker)
    {
        Entities.Add(Worker);
        _OnEntityCreated.ForEach(_ => _.Invoke(Worker));
        _OnWorkersChanged?.Invoke();
    }
    
    protected override void StartServiceInternal()
    {
        Game.RunAfterServicesInit((MeshFactory Factory, SaveGameManager Manager) =>
        {
            if (Manager.HasDataFor(SaveGameType.Workers))
                return;

            for (int i = 0; i < AttributeSet.Get()[AttributeType.AmountStartWorkers].CurrentValue; i++)
            {
                TryCreateNewEntity((int)UnitEntity.UType.Worker, null);
            }
            _OnInit?.Invoke(this);
        });
    }

    public override bool TryGetIdleEntity(out StarvableUnitEntity Entity)
    {
        // workers can be idle but without an assignable building
        if (!base.TryGetIdleEntity(out Entity))
            return false;

        if (!Game.TryGetService(out BuildingService Buildings))
            return false;

        foreach (var Building in Buildings.Entities)
        {
            // the building is not malaised and has idle spots
            if (Building.IsAboutToBeMalaised())
                continue;

            if (!Building.IsIdle())
                continue;

            return true;
        }
        Entity = default;
        return false;
    }

    protected override void StopServiceInternal() {}

    public override void OnAfterLoaded()
    {
        base.OnAfterLoaded();
        _OnInit?.Invoke(this);
        _OnWorkersChanged?.Invoke();
    }
    public override int GetAmountOfType(int EntityCode)
    {
        return Entities.Count;
    }

    protected override void ResetInternal()
    {
        
    }

    public delegate void OnWorkersChanged();
    public delegate void OnWorkersAssigned(LocationSet Locations);
    public static OnWorkersChanged _OnWorkersChanged;
    public static OnWorkersAssigned _OnWorkersAssigned;

    public static ActionList<StarvableUnitEntity> _OnWorkerAssignedList = new();
}
