using System;
using System.Collections;
using System.Collections.Generic;
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
        IncreaseYield
    }

    public Type EffectType = Type.YieldPerWorker;
    public HexagonConfig.HexagonType TileType = 0;
    public BuildingData.Type BuildingType = BuildingData.Type.DEFAULT;
    public Production Production = new Production();
    public int Range = 1;
    public float ProductionIncrease = 1.2f;
    public bool IsProductionBlockedByBuilding = false;

    public string GetDescription()
    {
        return "FILL OUT DESCRIPTION GEN";
    }

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

    public int GetSize()
    {
        return 4 + sizeof(int) + sizeof(float) + Production.GetSize();
    }

    public byte[] GetData()
    {
        NativeArray<byte> Bytes = new(GetSize(), Allocator.Temp);
        int Pos = 0;
        Pos = SaveGameManager.AddEnumAsInt(Bytes, Pos, (int)EffectType);
        Pos = SaveGameManager.AddEnumAsInt(Bytes, Pos, (int)TileType);
        Pos = SaveGameManager.AddEnumAsInt(Bytes, Pos, (int)BuildingType);
        Pos = SaveGameManager.AddBool(Bytes, Pos, IsProductionBlockedByBuilding);
        Pos = SaveGameManager.AddSaveable(Bytes, Pos, Production);
        Pos = SaveGameManager.AddInt(Bytes, Pos, Range);
        Pos = SaveGameManager.AddDouble(Bytes, Pos, ProductionIncrease);

        return Bytes.ToArray();
    }

    public void SetData(NativeArray<byte> Bytes)
    {
        int Pos = 0;
        Pos = SaveGameManager.GetEnumAsInt(Bytes, Pos, out int iEffectType);
        Pos = SaveGameManager.GetEnumAsInt(Bytes, Pos, out int iTileType);
        Pos = SaveGameManager.GetEnumAsInt(Bytes, Pos, out int iBuildingType);
        Pos = SaveGameManager.GetBool(Bytes, Pos, out IsProductionBlockedByBuilding);
        Pos = SaveGameManager.SetSaveable(Bytes, Pos, Production);
        Pos = SaveGameManager.GetInt(Bytes, Pos, out Range);
        Pos = SaveGameManager.GetDouble(Bytes, Pos, out double dProductionIncrease);

        EffectType = (Type)iEffectType;
        TileType = (HexagonConfig.HexagonType)iTileType;
        BuildingType = (BuildingData.Type)iBuildingType;
        ProductionIncrease = (float)dProductionIncrease;
    }
}