using System.Collections.Generic;
using UnityEngine;

public class Workers : UnitProvider<StarvableUnitData>
{
    public void RequestAddWorkerFor(BuildingData Building, int i) {
        if (!HasUnemployedWorkers())
        {
            MessageSystemScreen.CreateMessage(Message.Type.Error, "No idle worker exist for this building");
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
        _OnWorkersAssigned?.Invoke(Building.Location);
    }

    public void AssignWorkerTo(WorkerData Worker, BuildingData Building, int i)
    {
        Worker.AssignToBuilding(Building, i);
        Building.PutWorkerAt(Worker, i);
        _OnWorkersChanged?.Invoke();
        _OnWorkersAssigned?.Invoke(Building.Location);
    }

    public void KillWorker(WorkerData WorkerUnit)
    {
        if (WorkerUnit == null)
            return;

        if (WorkerUnit.IsEmployed())
        {
            RequestRemoveWorkerFor(WorkerUnit.GetAssignedBuilding(), WorkerUnit, WorkerUnit.GetAssignedBuildingSlot());
        }
        Units.Remove(WorkerUnit);
        _OnWorkersChanged?.Invoke();
        CheckForGameOver();
    }

    private void CheckForGameOver()
    {
        if (Units.Count != 0)
            return;

        if (!Game.TryGetService(out Units UnitService))
            return;

        if (UnitService.Units.Count != 0)
            return;

        Game.Instance.GameOver("Your tribe has died out!");
    }

    private WorkerData GetUnemployedWorker()
    {
        foreach (WorkerData WorkerUnit in Units)
        {
            if (!WorkerUnit.IsEmployed())
                return WorkerUnit;
        }
        return null;
    }

    public int GetEmployedWorkerCount() {
        int EmployedCount = 0;
        foreach (WorkerData WorkerUnit in Units)
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
        return Units.Count;
    }

    public void CreateNewWorker()
    {
        if (!Game.TryGetService(out MeshFactory MeshFactory))
            return;

        WorkerData Worker = (WorkerData)MeshFactory.CreateDataFromType(UnitData.UnitType.Worker);
        AddWorker(Worker);
    }

    public void AddWorker(WorkerData Worker)
    {
        Units.Add(Worker);
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

    public delegate void OnWorkersChanged();
    public delegate void OnWorkersAssigned(Location Location);
    public static OnWorkersChanged _OnWorkersChanged;
    public static OnWorkersAssigned _OnWorkersAssigned;
}
