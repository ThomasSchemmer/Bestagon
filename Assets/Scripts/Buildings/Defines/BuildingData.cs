using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using static CardUpgradeScreen;

[CreateAssetMenu(fileName = "Building", menuName = "ScriptableObjects/Building", order = 1)]
public class BuildingData : ScriptableObject, ISaveable
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
    public int WorkerCount = 0;
    public Type BuildingType = Type.DEFAULT;
    public Production Cost = new Production();
    public OnTurnBuildingEffect Effect = null;
    public int MaxWorker = 1;
    public HexagonConfig.HexagonType BuildableOn = 0;
    public int CurrentUsages = 1;
    public int MaxUsages = 1;

    public int UpgradeMaxWorker = 1;
    public HexagonConfig.HexagonType UpgradeBuildableOn = 0;
    public int UpgradeMaxUsages = 1;

    public BuildingData() {
        Location = Location.Zero;
        WorkerCount = 0;
        Cost = new();
        Effect = new();
    }

    public virtual Vector3 GetOffset() {
        return new Vector3(0, 5, 0);
    }

    public virtual Quaternion GetRotation() {
        return Quaternion.Euler(0, 180, 0);
    }

    public Production GetCosts()
    {
        return Cost;
    }

    public Production GetProduction()
    {
        return Effect.GetProduction(GetWorkerMultiplier(), Location);
    }

    public bool TryGetAdjacencyBonus(out Dictionary<HexagonConfig.HexagonType, Production> Bonus)
    {
        return Effect.TryGetAdjacencyBonus(out Bonus);
    }

    public bool CanBeBuildOn(HexagonVisualization Hex) {
        if (!Hex)
            return false;

        if (Hex.Data == null)
            return false;

        if (!BuildableOn.HasFlag(Hex.Data.Type))
            return false;

        if (Hex.Data.GetDiscoveryState() != HexagonData.DiscoveryState.Visited)
            return false;

        if (!Game.TryGetServices(out Stockpile Stockpile, out MapGenerator MapGenerator))
            return false;

        if (!Stockpile.CanAfford(GetCosts()))
            return false;

        if (MapGenerator.IsBuildingAt(Hex.Location))
            return false;

        if (Hex.Data.bIsMalaised)
            return false;

        return true;
    }

    public int GetWorkerMultiplier() {
        return WorkerCount;
    }

    public void RemoveWorker()
    {
        if (!Game.TryGetService(out Workers Workers))
            return;

        WorkerCount--;
        Workers.ReleaseWorkerFrom(this);
    }

    public void AddWorker() {
        if (WorkerCount >= MaxWorker)
            return;

        if (!Game.TryGetService(out Workers Workers))
            return;

        if (!Workers.RequestWorkerFor(this))
            return;

        WorkerCount++;
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

    public bool IsNeighbourBuildingBlocking()
    {
        return Effect.IsProductionBlockedByBuilding;
    }

    public void Upgrade(UpgradeableAttributes SelectedAttribute)
    {
        switch (SelectedAttribute)
        {
            case UpgradeableAttributes.MaxUsages:
                MaxUsages = Mathf.Clamp(MaxUsages + 1, 0, UpgradeMaxUsages);
                CurrentUsages = MaxUsages;
                return;
            case UpgradeableAttributes.MaxWorker:
                MaxWorker = Mathf.Clamp(MaxWorker + 1, 0, UpgradeMaxWorker); 
                return;
        }
    }

    public int GetSize()
    {
        return GetStaticSize();
    }

    public static int GetStaticSize()
    {
        // Type and buildable on as well as count Workers, max workers
        return Location.GetStaticSize() + Production.GetStaticSize() + 1 + OnTurnBuildingEffect.GetStaticSize() + sizeof(int) * 8;
    }

    public byte[] GetData()
    {
        NativeArray<byte> Bytes = new(GetSize(), Allocator.Temp);
        int Pos = 0;
        Pos = SaveGameManager.AddSaveable(Bytes, Pos, Location);
        Pos = SaveGameManager.AddSaveable(Bytes, Pos, Cost);
        Pos = SaveGameManager.AddEnumAsInt(Bytes, Pos, (int)BuildingType);
        Pos = SaveGameManager.AddInt(Bytes, Pos, (int)BuildableOn);
        Pos = SaveGameManager.AddSaveable(Bytes, Pos, Effect);
        Pos = SaveGameManager.AddInt(Bytes, Pos, MaxWorker);
        Pos = SaveGameManager.AddInt(Bytes, Pos, WorkerCount);
        Pos = SaveGameManager.AddInt(Bytes, Pos, CurrentUsages);
        Pos = SaveGameManager.AddInt(Bytes, Pos, MaxUsages);
        Pos = SaveGameManager.AddInt(Bytes, Pos, UpgradeMaxWorker);
        Pos = SaveGameManager.AddInt(Bytes, Pos, (int)UpgradeBuildableOn);
        Pos = SaveGameManager.AddInt(Bytes, Pos, UpgradeMaxUsages);


        return Bytes.ToArray();
    }

    public void SetData(NativeArray<byte> Bytes)
    {
        int Pos = 0;

        Pos = SaveGameManager.SetSaveable(Bytes, Pos, Location);
        Pos = SaveGameManager.SetSaveable(Bytes, Pos, Cost);
        Pos = SaveGameManager.GetEnumAsInt(Bytes, Pos, out int iBuildingType);
        Pos = SaveGameManager.GetInt(Bytes, Pos, out int iBuildableOn);
        Pos = SaveGameManager.SetSaveable(Bytes, Pos, Effect);
        Pos = SaveGameManager.GetInt(Bytes, Pos, out MaxWorker);
        Pos = SaveGameManager.GetInt(Bytes, Pos, out WorkerCount);
        Pos = SaveGameManager.GetInt(Bytes, Pos, out CurrentUsages);
        Pos = SaveGameManager.GetInt(Bytes, Pos, out MaxUsages);
        Pos = SaveGameManager.GetInt(Bytes, Pos, out UpgradeMaxWorker);
        Pos = SaveGameManager.GetInt(Bytes, Pos, out int iUpgradeBuildableOn);
        Pos = SaveGameManager.GetInt(Bytes, Pos, out UpgradeMaxUsages);

        BuildingType = (Type)iBuildingType;
        BuildableOn = (HexagonConfig.HexagonType)iBuildableOn;
        UpgradeBuildableOn = (HexagonConfig.HexagonType)iUpgradeBuildableOn;
    }

}
