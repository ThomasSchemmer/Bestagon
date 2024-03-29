using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;
using UnityEditor.Build.Content;
using UnityEngine;

[Serializable]
public class OnTurnBuildingEffect : BuildingEffect, ISaveable
{
    public enum Type
    {
        None,
        YieldPerWorker,
        YieldPerAreaAndWorker,
        YieldWorkerPerWorker,
        IncreaseYield,
    }

    public Type EffectType = Type.YieldPerWorker;
    public HexagonConfig.HexagonType TileType = 0;
    public Production Production = new Production();
    public int Range = 1;
    public float ProductionIncrease = 1.2f;
    public bool IsProductionBlockedByBuilding = false;

    public HexagonConfig.HexagonType UpgradeTileType = 0;
    public Production UpgradeProduction = new Production();
    public int UpgradeRange = 1;
    public float UpgradeProductionIncrease = 1.2f;


    public Production GetProduction(int Worker, Location Location)
    {
        switch(EffectType)
        {
            case Type.YieldPerWorker:
                return GetProductionPerWorker(Worker);
            case Type.YieldPerAreaAndWorker:
                return GetProductionPerAreaAndWorker(Worker, Location);

            default: return new();
        }
    }

    private Production GetProductionPerWorker(int Worker) { 
        return Production * Worker;
    }

    private Production GetProductionPerAreaAndWorker(int Worker, Location Location) {
        if (!Game.TryGetService(out MapGenerator MapGenerator))
            return new();

        List<HexagonData> NeighbourData = MapGenerator.GetNeighboursData(Location, Range);
        Production Production = new();

        if (!TryGetAdjacencyBonus(out Dictionary<HexagonConfig.HexagonType, Production> Bonus))
            return Production;

        foreach (HexagonData Data in NeighbourData)
        {
            if (MapGenerator.IsBuildingAt(Data.Location) && IsProductionBlockedByBuilding)
                continue;

            if (Bonus.TryGetValue(Data.Type, out Production AdjacentProduction))
            {
                Production += AdjacentProduction;
            }
        }
        return Production * Worker;
    }

    public bool TryGetAdjacencyBonus(out Dictionary<HexagonConfig.HexagonType, Production> Bonus)
    {
        Bonus = new Dictionary<HexagonConfig.HexagonType, Production>();
        if (EffectType != Type.YieldPerAreaAndWorker)
            return false;

        foreach (var RawEnum in Enum.GetValues(typeof(HexagonConfig.HexagonType))) {
            HexagonConfig.HexagonType Type = (HexagonConfig.HexagonType)RawEnum;
            if (TileType.HasFlag(Type))
                Bonus.Add(Type, Production);
        }
        return true;
    }

    private Production GetProductionPerWorkerPerWorker() { return new(); }

    private Production GetProductionIncreaseYield()
    {
        return new Production();
    }

    public string GetDescription()
    {
        switch (EffectType)
        {
            case Type.YieldPerWorker: return "Produces " + Production.GetShortDescription() + " per Worker and turns";
            case Type.YieldPerAreaAndWorker: return GetDescriptionYieldAreaWorker();
            case Type.YieldWorkerPerWorker: return "Creates X worker if Y worker occupy for a turn";
            case Type.IncreaseYield: return GetDescriptionIncreaseYield();

            default: return "No effect";
        }
    }

    private string GetDescriptionYieldAreaWorker()
    {
        return "Produces " + Production.GetShortDescription() + " per Worker for each surrounding " + HexagonConfig.GetShortTypeDescription(TileType);
    }

    private string GetDescriptionIncreaseYield()
    {
        return "Increases production of neighbouring X by Y";
    }

    public int GetSize()
    {
        return GetStaticSize();
    }

    public static int GetStaticSize()
    {
        return 2 + sizeof(int) * 4 + sizeof(double) * 2 + Production.GetStaticSize() * 2;
    }

    public byte[] GetData()
    {
        NativeArray<byte> Bytes = new(GetSize(), Allocator.Temp);
        int Pos = 0;
        Pos = SaveGameManager.AddEnumAsInt(Bytes, Pos, (int)EffectType);
        Pos = SaveGameManager.AddInt(Bytes, Pos, (int)TileType);
        Pos = SaveGameManager.AddBool(Bytes, Pos, IsProductionBlockedByBuilding);
        Pos = SaveGameManager.AddSaveable(Bytes, Pos, Production);
        Pos = SaveGameManager.AddInt(Bytes, Pos, Range);
        Pos = SaveGameManager.AddDouble(Bytes, Pos, ProductionIncrease);

        Pos = SaveGameManager.AddInt(Bytes, Pos, (int)UpgradeTileType);
        Pos = SaveGameManager.AddSaveable(Bytes, Pos, UpgradeProduction);
        Pos = SaveGameManager.AddInt(Bytes, Pos, UpgradeRange);
        Pos = SaveGameManager.AddDouble(Bytes, Pos, UpgradeProductionIncrease);

        return Bytes.ToArray();
    }

    public void SetData(NativeArray<byte> Bytes)
    {
        int Pos = 0;
        Pos = SaveGameManager.GetEnumAsInt(Bytes, Pos, out int iEffectType);
        Pos = SaveGameManager.GetInt(Bytes, Pos, out int iTileType);
        Pos = SaveGameManager.GetBool(Bytes, Pos, out IsProductionBlockedByBuilding);
        Pos = SaveGameManager.SetSaveable(Bytes, Pos, Production);
        Pos = SaveGameManager.GetInt(Bytes, Pos, out Range);
        Pos = SaveGameManager.GetDouble(Bytes, Pos, out double dProductionIncrease);

        Pos = SaveGameManager.GetInt(Bytes, Pos, out int iUpgradeTileType);
        Pos = SaveGameManager.SetSaveable(Bytes, Pos, UpgradeProduction);
        Pos = SaveGameManager.GetInt(Bytes, Pos, out UpgradeRange);
        Pos = SaveGameManager.GetDouble(Bytes, Pos, out double dUpgradeProductionIncrease);

        EffectType = (Type)iEffectType;
        TileType = (HexagonConfig.HexagonType)iTileType;
        ProductionIncrease = (float)dProductionIncrease;
        UpgradeTileType = (HexagonConfig.HexagonType)iUpgradeTileType;
        UpgradeProductionIncrease = (float)dUpgradeProductionIncrease;
    }
}