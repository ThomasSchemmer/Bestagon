using System.Collections.Generic;
using UnityEngine;

public class Workers : GameService
{
    // todo: save
    public void RequestAddWorkerFor(BuildingData Building, int i) {
        if (!HasUnemployedWorkers())
        {
            MessageSystem.CreateMessage(Message.Type.Error, "No idle worker exist for this building");
            return;
        }

        WorkerData WorkerUnit = GetUnemployedWorker();
        AssignWorkerTo(WorkerUnit, Building, i);
    }

    public void RequestRemoveWorkerFor(BuildingData Building, WorkerData Worker, int i)
    {
        Worker.RemoveFromBuilding();
        Building.RemoveWorker(i);
        _OnWorkersChanged?.Invoke();
    }

    public void AssignWorkerTo(WorkerData Worker, BuildingData Building, int i)
    {
        Worker.AssignToBuilding(Building, i);
        Building.PutWorkerAt(Worker, i);
        _OnWorkersChanged?.Invoke();
    }

    public void KillWorker(WorkerData WorkerUnit)
    {
        if (WorkerUnit == null)
            return;

        if (WorkerUnit.IsEmployed())
        {
            RequestRemoveWorkerFor(WorkerUnit.GetAssignedBuilding(), WorkerUnit, WorkerUnit.GetAssignedBuildingSlot());
        }
        ActiveWorkers.Remove(WorkerUnit);
        _OnWorkersChanged?.Invoke();
        CheckForGameOver();
    }

    private void CheckForGameOver()
    {
        if (ActiveWorkers.Count != 0)
            return;

        if (!Game.TryGetService(out Units Units))
            return;

        if (Units.ActiveUnits.Count != 0)
            return;

        Game.Instance.GameOver("Your tribe has died out!");
    }

    private WorkerData GetUnemployedWorker()
    {
        foreach (WorkerData WorkerUnit in ActiveWorkers)
        {
            if (!WorkerUnit.IsEmployed())
                return WorkerUnit;
        }
        return null;
    }

    public int GetEmployedWorkerCount() {
        int EmployedCount = 0;
        foreach (WorkerData WorkerUnit in ActiveWorkers)
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
        return ActiveWorkers.Count;
    }

    public void CreateNewWorker()
    {
        if (!Game.TryGetService(out MeshFactory MeshFactory))
            return;

        WorkerData Worker = (WorkerData)MeshFactory.CreateDataFromType(UnitData.UnitType.Worker);
        ActiveWorkers.Add(Worker);
    }
    
    protected override void StartServiceInternal()
    {
        Game.RunAfterServiceInit((MeshFactory Factory) =>
        {
            CreateNewWorker();
            CreateNewWorker();
            _OnInit?.Invoke();
        });
    }

    protected override void StopServiceInternal() {}

    public List<WorkerData> ActiveWorkers = new();

    public delegate void OnWorkersChanged();
    public OnWorkersChanged _OnWorkersChanged;
}
