using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

[Serializable]
public class OnTurnBuildingEffect : BuildingEffect, ISaveableData
{
    public enum Type
    {
        None,
        Produce,
        ConsumeProduce,
        ProduceUnit,
        Merchant,
    }

    public Type EffectType = Type.Produce;
    public UnitEntity.UType UnitType = UnitEntity.UType.Worker;
    public HexagonConfig.HexagonType TileType = 0;
    public Production Production = new Production();
    public Production Consumption = new Production();
    public int Range = 0;

    public HexagonConfig.HexagonType UpgradeTileType = 0;
    public Production UpgradeProduction = new Production();
    public int UpgradeRange = 0;


    public Production GetProduction(int Worker, Location Location, bool bIsSimulated)
    {
        switch(EffectType)
        {
            case Type.Produce:
                return GetProductionAt(Worker, Location);
            case Type.ConsumeProduce:
                return GetConsumeProductionAt(Worker, Location, bIsSimulated);
            // ProduceUnit is handled on its own
            default: return new();
        }
    }

    private Production GetProductionAt(int Worker, Location Location) {
        if (!Game.TryGetServices(out MapGenerator MapGenerator, out BuildingService Buildings))
            return new();

        bool bShouldAddOrigin = Range == 0;
        List<HexagonData> NeighbourData = MapGenerator.GetNeighboursData(Location, bShouldAddOrigin, Range);
        Production Production = new();

        if (!TryGetAdjacencyBonus(out Dictionary<HexagonConfig.HexagonType, Production> Bonus))
            return Production;

        foreach (HexagonData Data in NeighbourData)
        {
            if (Bonus.TryGetValue(Data.Type, out Production AdjacentProduction))
            {
                Production += AdjacentProduction;
            }
        }
        return Production * Worker;
    }

    private Production GetConsumeProductionAt(int Worker, Location Location, bool bIsSimulated)
    {
        if (!Game.TryGetService(out Stockpile Stockpile))
            return new();

        Production Resources = bIsSimulated ? Stockpile.SimulatedResources : Stockpile.Resources;

        int MaxAmount = Worker;
        foreach (var Tuple in Consumption.GetTuples())
        {
            if (Tuple.Value == 0)
                continue;

            int Amount = Mathf.Max(Resources[Tuple.Key], 0) / Consumption[Tuple.Key];
            MaxAmount = Mathf.Min(Amount, MaxAmount);
        }

        Production Combined = MaxAmount * Production;
        Combined -= MaxAmount * Consumption;
        return Combined;
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
            case Type.Merchant: return "Allows trading resources for new cards";

            default: return "No effect";
        }
    }

    public GameObject GetEffectVisuals(ISelectable Parent)
    {
        if (!Game.TryGetService(out IconFactory IconFactory))
            return null;

        switch (EffectType)
        {
            case Type.Produce: return IconFactory.GetVisualsForProduceEffect(this, Parent);
            case Type.ProduceUnit: return IconFactory.GetVisualsForProduceUnitEffect(this, Parent);
            case Type.ConsumeProduce: return IconFactory.GetVisualsForProduceConsumeEffect(this, Parent);
            case Type.Merchant: return IconFactory.GetVisualsForMerchantEffect(this, Parent);
            default: return null;
        }
    }

    private string GetDescriptionProduce()
    {
        return Range == 0 ? "if built on " : "per adjacent";
    }

    private string GetDescriptionConsumeProduce()
    {
        return "by consuming";
    }

    private string GetDescriptionProduceUnit()
    {
        return "Produces with two workers";
    }

    public string GetDescriptionProduceUnitConsumption()
    {
        return "and consumes";
    }

    public int GetSize()
    {
        return GetStaticSize();
    }

    public static int GetStaticSize()
    {
        return 1 + sizeof(int) * 4 + Production.GetStaticSize() * 3;
    }

    public byte[] GetData()
    {
        NativeArray<byte> Bytes = new(GetSize(), Allocator.Temp);
        int Pos = 0;
        Pos = SaveGameManager.AddEnumAsByte(Bytes, Pos, (byte)EffectType);
        Pos = SaveGameManager.AddInt(Bytes, Pos, (int)TileType);
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
        Pos = SaveGameManager.GetEnumAsByte(Bytes, Pos, out byte bEffectType);
        Pos = SaveGameManager.GetInt(Bytes, Pos, out int iTileType);
        Pos = SaveGameManager.SetSaveable(Bytes, Pos, Production);
        Pos = SaveGameManager.SetSaveable(Bytes, Pos, Consumption);
        Pos = SaveGameManager.GetInt(Bytes, Pos, out Range);

        Pos = SaveGameManager.GetInt(Bytes, Pos, out int iUpgradeTileType);
        Pos = SaveGameManager.SetSaveable(Bytes, Pos, UpgradeProduction);
        Pos = SaveGameManager.GetInt(Bytes, Pos, out UpgradeRange);

        EffectType = (Type)bEffectType;
        TileType = (HexagonConfig.HexagonType)iTileType;
        UpgradeTileType = (HexagonConfig.HexagonType)iUpgradeTileType;
    }
}