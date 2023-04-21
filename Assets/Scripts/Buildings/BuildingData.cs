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
        foreach (Worker Worker in Workers) {
            if (Worker == null)
                continue;

            Count++;
        }

        return Count;
    }

    public Worker GetWorkerAt(int i) {
        if (i >= Workers.Count)
            return null;

        return Workers[i];
    }

    public Worker GetRandomWorker() {
        return GetWorkerAt(Random.Range(0, Workers.Count));
    }

    public Worker RemoveWorkerAt(int i) {
        Worker RemovedWorker = GetWorkerAt(i);
        if (RemovedWorker != null) {
            RemovedWorker.AssignedBuilding = null;
            Workers.Remove(RemovedWorker);
        }
        return RemovedWorker;
    }

    public Worker RemoveRandomWorker() {
        Worker RemovedWorker = GetRandomWorker();
        if (RemovedWorker != null) {
            RemovedWorker.AssignedBuilding = null;
            Workers.Remove(RemovedWorker);
        }
        return RemovedWorker;
    }

    public void RemoveWorker(Worker Worker) {
        if (Worker == null)
            return;

        // avoid modifying while looping
        Worker Target = null;
        foreach (Worker AssignedWorker in Workers) {
            if (AssignedWorker != Worker)
                continue;

            Target = AssignedWorker;
            break;
        }

        if (Target != null) {
            Target.AssignedBuilding = null;
            Workers.Remove(Target);
        }
    }

    public void AddWorker(Worker Worker) {
        Workers.Add(Worker);
        Worker.AssignedBuilding = this;
    }



    public Location Location;
    public List<Worker> Workers = new();
}
