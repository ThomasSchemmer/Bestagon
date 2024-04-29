﻿using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using static CardUpgradeScreen;

[CreateAssetMenu(fileName = "Building", menuName = "ScriptableObjects/Building", order = 1)]
public class BuildingData : ScriptableObject, ISaveable
{
    public Location Location;
    public WorkerData[] AssignedWorkers;
    public BuildingConfig.Type BuildingType = BuildingConfig.Type.DEFAULT;
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
        Init();
        Cost = new();
        Effect = new();
    }

    public void Init()
    {
        Location = Location.Zero;
        AssignedWorkers = new WorkerData[MaxWorker];
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

    public Production GetProductionPreview(Location Location)
    {
        return Effect.GetProduction(GetAssignedWorkerCount(), Location);
    }

    public bool TryGetAdjacencyBonus(out Dictionary<HexagonConfig.HexagonType, Production> Bonus)
    {
        return Effect.TryGetAdjacencyBonus(out Bonus);
    }

    public bool CanBeBuildOn(HexagonVisualization Hex, bool bShouldCheckCosts = true) {
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

        if (bShouldCheckCosts && !Stockpile.CanAfford(GetCosts()))
            return false;

        if (MapGenerator.IsBuildingAt(Hex.Location))
            return false;

        if (Hex.Data.bIsMalaised)
            return false;

        return true;
    }

    public int GetAssignedWorkerCount() {
        return AssignedWorkers.Length;
    }

    public int GetWorkingWorkerCount()
    {
        int WorkerCount = 0;
        for (int i = 0; i < AssignedWorkers.Length; i++)
        {
            WorkerCount += AssignedWorkers[i] != null && !AssignedWorkers[i].IsStarving() ? 1 : 0;
        }
        return WorkerCount;
    }

    public int GetWorkerMultiplier() {
        return GetWorkingWorkerCount();
    }

    public void RequestAddWorkerAt(int i) {
        if (AssignedWorkers[i] != null)
            return;

        if (!Game.TryGetService(out Workers Workers))
            return;

        Workers.RequestAddWorkerFor(this, i);
    }

    public void RequestRemoveWorkerAt(int i)
    {
        if (AssignedWorkers[i] == null)
            return;

        if (!Game.TryGetService(out Workers Workers))
            return;

        Workers.RequestRemoveWorkerFor(this, AssignedWorkers[i], i);
    }

    public void PutWorkerAt(WorkerData Worker, int i)
    {
        if (AssignedWorkers[i] != null)
            return;

        AssignedWorkers[i] = Worker;
    }

    public void RemoveWorker(int i)
    {
        AssignedWorkers[i] = null;
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
        if (!IsUpgradePossible(SelectedAttribute))
            return;

        switch (SelectedAttribute)
        {
            case UpgradeableAttributes.MaxUsages:
                MaxUsages = Mathf.Clamp(MaxUsages + 1, 0, UpgradeMaxUsages);
                CurrentUsages = MaxUsages;
                return;
            case UpgradeableAttributes.MaxWorker:
                MaxWorker = Mathf.Clamp(MaxWorker + 1, 0, UpgradeMaxWorker); 
                return;
            case UpgradeableAttributes.Production:
                UpgradeProduction();
                return;
        }
    }

    private void UpgradeProduction()
    {
        Production Difference = Effect.UpgradeProduction - Effect.Production;
        foreach (var Tuple in Difference.GetTuples())
        {
            if (Tuple.Value == 0)
                continue;

            Effect.Production[Tuple.Key]++;
            return;
        }
    }

    public bool IsUpgradePossible(UpgradeableAttributes SelectedAttribute)
    {
        switch (SelectedAttribute)
        {
            case UpgradeableAttributes.MaxUsages:
                return MaxUsages < UpgradeMaxUsages;
            case UpgradeableAttributes.MaxWorker:
                return MaxWorker < UpgradeMaxWorker;
            case UpgradeableAttributes.Production:
                return Effect.Production < Effect.UpgradeProduction;
        }
        return false;
    }

    public int GetSize()
    {
        return GetStaticSize();
    }

    public static int GetStaticSize()
    {
        // Type and buildable on, max workers
        // Workers themselfs will be assigned later
        return Location.GetStaticSize() + Production.GetStaticSize() + 1 + OnTurnBuildingEffect.GetStaticSize() + sizeof(int) * 7;
    }

    public byte[] GetData()
    {
        NativeArray<byte> Bytes = new(GetSize(), Allocator.Temp);
        int Pos = 0;
        Pos = SaveGameManager.AddSaveable(Bytes, Pos, Location);
        Pos = SaveGameManager.AddSaveable(Bytes, Pos, Cost);
        Pos = SaveGameManager.AddEnumAsInt(Bytes, Pos, HexagonConfig.MaskToInt((int)BuildingType, 32));
        Pos = SaveGameManager.AddInt(Bytes, Pos, (int)BuildableOn);
        Pos = SaveGameManager.AddSaveable(Bytes, Pos, Effect);
        Pos = SaveGameManager.AddInt(Bytes, Pos, MaxWorker);
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
        Pos = SaveGameManager.GetInt(Bytes, Pos, out CurrentUsages);
        Pos = SaveGameManager.GetInt(Bytes, Pos, out MaxUsages);
        Pos = SaveGameManager.GetInt(Bytes, Pos, out UpgradeMaxWorker);
        Pos = SaveGameManager.GetInt(Bytes, Pos, out int iUpgradeBuildableOn);
        Pos = SaveGameManager.GetInt(Bytes, Pos, out UpgradeMaxUsages);

        BuildingType = (BuildingConfig.Type)HexagonConfig.IntToMask(iBuildingType);
        BuildableOn = (HexagonConfig.HexagonType)iBuildableOn;
        UpgradeBuildableOn = (HexagonConfig.HexagonType)iUpgradeBuildableOn;
    }

}
