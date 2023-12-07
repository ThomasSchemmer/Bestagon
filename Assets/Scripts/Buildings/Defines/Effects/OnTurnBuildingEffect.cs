using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class OnTurnBuildingEffect : BuildingEffect
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
    public HexagonConfig.HexagonType TileType = HexagonConfig.HexagonType.DEFAULT;
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
}