using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;
using UnityEditor.Build.Content;
using UnityEngine;
using UnityEngine.XR;

[Serializable]
public class OnTurnBuildingEffect : BuildingEffect, ISaveable
{
    public enum Type
    {
        None,
        Produce,
        ConsumeProduce,
        ProduceUnit,
    }

    public Type EffectType = Type.Produce;
    public HexagonConfig.HexagonType TileType = 0;
    public Production Production = new Production();
    public Production Consumption = new Production();
    public int Range = 0;
    public bool IsProductionBlockedByBuilding = false;

    public HexagonConfig.HexagonType UpgradeTileType = 0;
    public Production UpgradeProduction = new Production();
    public int UpgradeRange = 0;


    public Production GetProduction(int Worker, Location Location)
    {
        switch(EffectType)
        {
            case Type.Produce:
                return GetProductionAt(Worker, Location);
            case Type.ConsumeProduce:
                return GetConsumeProductionAt(Worker, Location);
            // ProduceUnit is handled on its own
            default: return new();
        }
    }

    private Production GetProductionAt(int Worker, Location Location) {
        if (!Game.TryGetService(out MapGenerator MapGenerator))
            return new();

        bool bShouldAddOrigin = Range == 0;
        List<HexagonData> NeighbourData = MapGenerator.GetNeighboursData(Location, bShouldAddOrigin, Range);
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

    private Production GetConsumeProductionAt(int Worker, Location Location)
    {
        if (!Game.TryGetService(out Stockpile Stockpile))
            return new();

        if (!Stockpile.CanAfford(Consumption))
            return new();

        Stockpile.Pay(Consumption);
        return Production * Worker;
    }

    public bool TryGetAdjacencyBonus(out Dictionary<HexagonConfig.HexagonType, Production> Bonus)
    {
        Bonus = new Dictionary<HexagonConfig.HexagonType, Production>();
        if (EffectType != Type.Produce)
            return false;

        foreach (var RawEnum in Enum.GetValues(typeof(HexagonConfig.HexagonType))) {
            HexagonConfig.HexagonType Type = (HexagonConfig.HexagonType)RawEnum;
            if (TileType.HasFlag(Type))
                Bonus.Add(Type, Production);
        }
        return true;
    }

    public string GetDescription()
    {
        switch (EffectType)
        {
            case Type.Produce: return GetDescriptionProduce();
            case Type.ProduceUnit: return GetDescriptionProduceUnit();
            case Type.ConsumeProduce: return GetDescriptionConsumeProduce();

            default: return "No effect";
        }
    }

    private string GetDescriptionProduce()
    {
        return "Produces " + Production.GetShortDescription() + " per Worker for each surrounding " + HexagonConfig.GetShortTypeDescription(TileType);
    }

    private string GetDescriptionConsumeProduce()
    {
        return "Produces X at the cost of Y";
    }

    private string GetDescriptionProduceUnit()
    {
        return "Creates X worker if Y worker occupy for a turn";
    }

    public int GetSize()
    {
        return GetStaticSize();
    }

    public static int GetStaticSize()
    {
        return 2 + sizeof(int) * 4 + Production.GetStaticSize() * 3;
    }

    public byte[] GetData()
    {
        NativeArray<byte> Bytes = new(GetSize(), Allocator.Temp);
        int Pos = 0;
        Pos = SaveGameManager.AddEnumAsInt(Bytes, Pos, (int)EffectType);
        Pos = SaveGameManager.AddInt(Bytes, Pos, (int)TileType);
        Pos = SaveGameManager.AddBool(Bytes, Pos, IsProductionBlockedByBuilding);
        Pos = SaveGameManager.AddSaveable(Bytes, Pos, Production);
        Pos = SaveGameManager.AddSaveable(Bytes, Pos, Consumption);
        Pos = SaveGameManager.AddInt(Bytes, Pos, Range);

        Pos = SaveGameManager.AddInt(Bytes, Pos, (int)UpgradeTileType);
        Pos = SaveGameManager.AddSaveable(Bytes, Pos, UpgradeProduction);
        Pos = SaveGameManager.AddInt(Bytes, Pos, UpgradeRange);

        return Bytes.ToArray();
    }

    public void SetData(NativeArray<byte> Bytes)
    {
        int Pos = 0;
        Pos = SaveGameManager.GetEnumAsInt(Bytes, Pos, out int iEffectType);
        Pos = SaveGameManager.GetInt(Bytes, Pos, out int iTileType);
        Pos = SaveGameManager.GetBool(Bytes, Pos, out IsProductionBlockedByBuilding);
        Pos = SaveGameManager.SetSaveable(Bytes, Pos, Production);
        Pos = SaveGameManager.SetSaveable(Bytes, Pos, Consumption);
        Pos = SaveGameManager.GetInt(Bytes, Pos, out Range);

        Pos = SaveGameManager.GetInt(Bytes, Pos, out int iUpgradeTileType);
        Pos = SaveGameManager.SetSaveable(Bytes, Pos, UpgradeProduction);
        Pos = SaveGameManager.GetInt(Bytes, Pos, out UpgradeRange);

        EffectType = (Type)iEffectType;
        TileType = (HexagonConfig.HexagonType)iTileType;
        UpgradeTileType = (HexagonConfig.HexagonType)iUpgradeTileType;
    }
}