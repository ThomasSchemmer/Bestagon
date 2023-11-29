using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Building", menuName = "ScriptableObjects/Building", order = 1)]
public class BuildingData : ScriptableObject
{
    [Flags]
    public enum Type
    {
        DEFAULT = 0,
        Woodcutter = 1 << 0,
        Farm = 1 << 1,
        Mine = 1 << 2,
        Quarry = 1 << 3,
        Hut = 1 << 4,
        House = 1 << 5,
        Sawmill = 1 << 6,
        Blacksmith = 1 << 7,
        Mill = 1 << 8,
        Stonemason = 1 << 9,
        Watchtower = 1 << 10,
        Witchhut = 1 << 11,
        Harbor = 1 << 12
    }

    public Location Location;
    public List<WorkerData> Workers = new();
    public Type BuildingType = Type.DEFAULT;
    public Production Cost = new Production();
    public OnTurnBuildingEffect Effect = null;
    public int MaxWorker = 1;
    public HexagonConfig.HexagonType BuildableOn = HexagonConfig.HexagonType.DEFAULT;

    public BuildingData() {
        Location = new Location(new Vector2Int(0, 0), new Vector2Int(0, 0));
    }

    public Production GetProduction() {
        return Effect.Production * GetWorkerMultiplier();
    }

    public virtual bool IsNeighbourBuildingBlocking() {
        return false;
    }

    public virtual Vector3 GetOffset() {
        return new Vector3(0, 5, 0);
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

    public Production GetCosts()
    {
        return Cost;
    }

    public bool CanBeBuildOn(HexagonVisualization Hex) {
        if (!Hex)
            return false;

        if (Hex.Data == null)
            return false;

        if (!BuildableOn.HasFlag(Hex.Data.Type))
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
        return GetWorkerAt(UnityEngine.Random.Range(0, Workers.Count));
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
}
