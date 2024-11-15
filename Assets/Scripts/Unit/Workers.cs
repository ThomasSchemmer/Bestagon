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

    public void CreateNewWorker()
    {
        if (!Game.TryGetService(out MeshFactory MeshFactory))
            return;

        WorkerEntity Worker = (WorkerEntity)MeshFactory.CreateDataFromType(UnitEntity.UType.Worker);
        AddWorker(Worker);
    }

    public void AddWorker(WorkerEntity Worker)
    {
        Entities.Add(Worker);
        _OnEntityCreated.ForEach(_ => _.Invoke(Worker));
    }
    
    protected override void StartServiceInternal()
    {
        Game.RunAfterServiceInit((MeshFactory Factory) =>
        {

            for (int i = 0; i < AttributeSet.Get()[AttributeType.AmountStartWorkers].CurrentValue; i++)
            {
                CreateNewWorker();
            }
            _OnInit?.Invoke(this);
        });
    }

    protected override void StopServiceInternal() {}

    public delegate void OnWorkersChanged();
    public delegate void OnWorkersAssigned(LocationSet Locations);
    public static OnWorkersChanged _OnWorkersChanged;
    public static OnWorkersAssigned _OnWorkersAssigned;

    public static ActionList<StarvableUnitEntity> _OnWorkerAssignedList = new();
}
