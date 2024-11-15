using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

[Serializable]
public class OnTurnBuildingEffect : Effect, ISaveableData
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
    public Production Production = new Production();
    public Production Consumption = new Production();
    public int Range = 0;

    public Production UpgradeProduction = new Production();
    public int UpgradeRange = 0;

    protected BuildingEntity Building;

    public void Init(BuildingEntity Building)
    {
        this.Building = Building;
    }

    public Production GetProduction(int Worker, LocationSet Location, bool bIsSimulated)
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

    private Production GetProductionAt(int Worker, LocationSet Location) {
        if (!Game.TryGetServices(out MapGenerator MapGenerator, out BuildingService Buildings))
            return new();

        bool bShouldAddOrigin = Range == 0;
        HashSet<HexagonData> NeighbourData = MapGenerator.GetNeighboursData(Location, bShouldAddOrigin, Range);
        Production Production = new();

        if (!TryGetAdjacencyBonus(out Dictionary<HexagonConfig.HexagonType, Production> Bonus))
            return Production;

        foreach (HexagonData Data in NeighbourData)
        {
            if (Bonus.TryGetValue(Data.Type, out Production AdjacentProduction))
            {
                float Multiplier = AttributeSet.Get()[AttributeType.ProductionRate].GetAt(Data.Location);
                Production += Multiplier * AdjacentProduction;
            }
        }

        return Production * Worker;
    }

    private Production GetConsumeProductionAt(int Worker, LocationSet Location, bool bIsSimulated)
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
        if (Building == null)
            return false;

        if (EffectType != Type.Produce)
            return false;

        foreach (var RawEnum in Enum.GetValues(typeof(HexagonConfig.HexagonType))) {
            HexagonConfig.HexagonType Type = (HexagonConfig.HexagonType)RawEnum;
            if (Building.BuildableOn.HasFlag(Type))
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
        if (Building == null)
            return null;

        if (!Game.TryGetService(out IconFactory IconFactory))
            return null;

        switch (EffectType)
        {
            case Type.Produce: return IconFactory.GetVisualsForProduceEffect(Building, Parent);
            case Type.ProduceUnit: return IconFactory.GetVisualsForProduceUnitEffect(this, Parent);
            case Type.ConsumeProduce: return IconFactory.GetVisualsForProduceConsumeEffect(Building, Parent);
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
        return 1 + sizeof(int) * 2 + Production.GetStaticSize() * 3;
    }

    public byte[] GetData()
    {
        NativeArray<byte> Bytes = new(GetSize(), Allocator.Temp);
        int Pos = 0;
        Pos = SaveGameManager.AddEnumAsByte(Bytes, Pos, (byte)EffectType);
        Pos = SaveGameManager.AddSaveable(Bytes, Pos, Production);
        Pos = SaveGameManager.AddSaveable(Bytes, Pos, Consumption);
        Pos = SaveGameManager.AddInt(Bytes, Pos, Range);

        Pos = SaveGameManager.AddSaveable(Bytes, Pos, UpgradeProduction);
        Pos = SaveGameManager.AddInt(Bytes, Pos, UpgradeRange);

        return Bytes.ToArray();
    }

    public void SetData(NativeArray<byte> Bytes)
    {
        int Pos = 0;
        Pos = SaveGameManager.GetEnumAsByte(Bytes, Pos, out byte bEffectType);
        Pos = SaveGameManager.SetSaveable(Bytes, Pos, Production);
        Pos = SaveGameManager.SetSaveable(Bytes, Pos, Consumption);
        Pos = SaveGameManager.GetInt(Bytes, Pos, out Range);

        Pos = SaveGameManager.SetSaveable(Bytes, Pos, UpgradeProduction);
        Pos = SaveGameManager.GetInt(Bytes, Pos, out UpgradeRange);

        EffectType = (Type)bEffectType;
    }
}