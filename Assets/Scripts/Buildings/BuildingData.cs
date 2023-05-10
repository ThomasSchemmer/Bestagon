using System.Collections.Generic;
using UnityEngine;

public abstract class BuildingData {
    public enum Type {
        Default,
        Woodcutter,
        Farm,
        Mine
    }

    public BuildingData() {
        Location = new Location(new Vector2Int(0, 0), new Vector2Int(0, 0));
    }

    public Production GetProduction() {
        return _GetProduction() * GetWorkerMultiplier();
    }

    protected abstract Production _GetProduction();

    public abstract Type GetBuildingType();

    public abstract int GetMaxWorker();

    public abstract Production GetCosts();

    public virtual bool IsNeighbourBuildingBlocking() {
        return false;
    }

    public virtual Vector3 GetOffset() {
        return new Vector3(0, 2, 0);
    }

    public virtual Quaternion GetRotation() {
        return Quaternion.Euler(0, 180, 0);
    }

    public Production GetAdjacencyProduction() {
        List<HexagonData> NeighbourData = MapGenerator.GetNeighboursData(Location);
        Production Production = new();

        if (!TryGetAdjacencyBonus(out Dictionary<HexagonConfig.HexagonType, Production>  Bonus))
            return Production;

        foreach (HexagonData Data in NeighbourData) {
            if (MapGenerator.IsBuildingAt(Data.Location) && IsNeighbourBuildingBlocking())
                continue;

            if (Bonus.TryGetValue(Data.Type, out Production AdjacentProduction)) {
                Production += AdjacentProduction;
            }
        }
        return Production * GetWorkerMultiplier();
    }

    public virtual bool TryGetAdjacencyBonus(out Dictionary<HexagonConfig.HexagonType, Production> Bonus) {
        Bonus = new Dictionary<HexagonConfig.HexagonType, Production>();
        return false;
    }

    public static BuildingData CreateFromType(Type Type) {
        switch (Type) {
            case Type.Mine: return new MineBuilding();
            case Type.Farm: return new FarmBuilding();
            default: return new WoodcutterBuilding();
        }
    }

    public virtual bool CanBeBuildOn(HexagonVisualization Hex) {
        // add additional checks in subclasses!

        if (!Hex)
            return false;

        if (Hex.Data == null)
            return false;

        return !MapGenerator.IsBuildingAt(Hex.Location) && !Hex.Data.bIsMalaised;
    }

    public int GetWorkerMultiplier() {
        int Count = 0;
        foreach (WorkerData Worker in Workers) {
            if (Worker == null || !Worker.Location.Equals(Location))
                continue;

            Count++;
        }

        return Count;
    }

    public WorkerData GetWorkerAt(int i) {
        if (i >= Workers.Count)
            return null;

        return Workers[i];
    }

    public WorkerData GetRandomWorker() {
        return GetWorkerAt(Random.Range(0, Workers.Count));
    }

    public WorkerData RemoveWorkerAt(int i) {
        WorkerData RemovedWorker = GetWorkerAt(i);
        if (RemovedWorker != null) {
            RemovedWorker.RemoveFromBuilding();
        }
        return RemovedWorker;
    }

    public WorkerData RemoveRandomWorker() {
        WorkerData RemovedWorker = GetRandomWorker();
        if (RemovedWorker != null) {
            RemovedWorker.RemoveFromBuilding();
        }
        return RemovedWorker;
    }

    public void RemoveWorker(WorkerData Worker) {
        if (Worker == null)
            return;

        Workers.Remove(Worker);
    }

    public void AddWorker(WorkerData Worker) {
        Workers.Add(Worker);
        Worker.AssignedBuilding = this;
    }

    public override bool Equals(object Other) {
        if (Other is not BuildingData) 
            return false;

        BuildingData OtherBuilding = (BuildingData)Other;
        return Location.Equals(OtherBuilding.Location);
    }

    public override int GetHashCode() {
        return Location.GetHashCode() + "Building".GetHashCode();
    }

    public Location Location;
    public List<WorkerData> Workers = new();
}
