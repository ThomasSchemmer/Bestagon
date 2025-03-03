using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

[Serializable]
public class OnTurnBuildingEffect : Effect
{
    public enum Type
    {
        None,
        Produce,
        ConsumeProduce,
        ProduceUnit,
        Scavenger,
        Library,
        Outpost
    }

    [SaveableEnum]
    public Type EffectType = Type.Produce;
    [SaveableEnum]
    public UnitEntity.UType UnitType = UnitEntity.UType.Worker;
    [SaveableClass]
    public Production Production = new Production();
    [SaveableClass]
    public Production Consumption = new Production();
    [SaveableBaseType]
    public int Range = 0;

    [SaveableClass]
    public Production UpgradeProduction = new Production();
    [SaveableBaseType]
    public int UpgradeRange = 0;

    protected BuildingEntity Building;

    public void Init(BuildingEntity Building)
    {
        this.Building = Building;
    }

    /**
     * Returns the production per turn of the effect
     * Has to be provided Location instead of using building location, as preview might not work
     * bIsMaximum: returns the maximum production if true, ignoring eg consumption costs
     */
    public Production GetProduction(int Worker, LocationSet Location, bool bIsSimulated, bool bIsMaximum)
    {
        switch(EffectType)
        {
            case Type.Produce:
                return GetProductionAt(Worker, Location);
            case Type.ConsumeProduce:
                return GetConsumeProductionAt(Worker, bIsSimulated, bIsMaximum);
            // ProduceUnit is handled on its own as well
            case Type.ProduceUnit:
                return GetUnitProductionCost(bIsSimulated);
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

    private Production GetConsumeProductionAt(int Worker, bool bIsSimulated, bool bIsMaximum)
    {
        if (!Game.TryGetService(out Stockpile Stockpile))
            return new();

        Production Resources = bIsSimulated ? Stockpile.SimulatedResources : Stockpile.Resources;

        int MaxAmount = Worker;
        if (bIsMaximum)
        {
            return MaxAmount * (Production - Consumption);
        }

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

    public Production GetUnitProductionCost(bool bIsSimulated)
    {
        return CanProduceUnit(bIsSimulated) ? Consumption : Production.Empty;
    }

    public void ProduceUnit()
    {
        EntityProvider Provider = GetUnitProvider();
        Provider.TryCreateNewEntity((int)UnitType, GetUnitSpawnLocation());
    }

    public void ResearchTurn()
    {
        if (!Game.TryGetService(out AmberService Ambers))
            return;

        Ambers.ResearchTurn();
    }

    private LocationSet GetUnitSpawnLocation()
    {
        if (this.EffectType != Type.ProduceUnit)
            return null;

        if (Building.ExtendableOn != 0)
        {
            return Building.GetLocations().GetExtendedLocation().ToSet();
        }
        return Building.GetLocations();
    }

    private EntityProvider GetUnitProvider()
    {
        switch (UnitType)
        {
            case UnitEntity.UType.Worker: return Game.GetService<Workers>();
            case UnitEntity.UType.Scout:
            case UnitEntity.UType.Boat: return Game.GetService<Units>();
            default: return null;
        }
    }

    public bool CanProduceUnit(bool bIsSimulated)
    {
        if (!Game.TryGetService(out Stockpile Stockpile) || Building == null)
            return false;

        Production Resources = bIsSimulated ? Stockpile.SimulatedResources : Stockpile.Resources;

        // can only ever produce one per turn
        int Worker = Building.GetWorkingWorkerCount(bIsSimulated);
        int MaxAmount = Worker == Building.GetMaximumWorkerCount() ? 1 : 0;
        foreach (var Tuple in Consumption.GetTuples())
        {
            if (Tuple.Value == 0)
                continue;

            int Amount = Mathf.Max(Resources[Tuple.Key], 0) / Consumption[Tuple.Key];
            MaxAmount = Mathf.Min(Amount, MaxAmount);
        }
        if (MaxAmount == 0)
            return false;

        if (!Game.TryGetService(out Units Units))
            return false;

        var Location = GetUnitSpawnLocation();
        return !Units.IsEntityAt(Location);
    }

    public bool CanResearchInLibrary(bool bIsIsSimulated)
    {
        if (!Game.TryGetServices(out AmberService Ambers, out Stockpile Stockpile))
            return false;

        if (!Stockpile.CanAfford(Consumption, bIsIsSimulated))
            return false;

        if (Building.GetWorkingWorkerCount(bIsIsSimulated) < Building.GetMaximumWorkerCount())
            return false;

        if (!Ambers.IsUnlocked())
            return true;

        return Ambers.CanHealMalaise();
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

    public void CorruptProduction(int State)
    {
        if (EffectType != Type.Produce && EffectType != Type.ConsumeProduce)
            return;

        Production = UpgradeProduction;
        Production = BuildingEntity.CorruptProduction(Production, State);
    }


    public void CorruptConsumptionCost(int State)
    {
        if (EffectType != Type.ConsumeProduce && EffectType != Type.ProduceUnit)
            return;

        Consumption = BuildingEntity.CorruptProduction(Consumption, State);
    }

    public string GetDescription()
    {
        switch (EffectType)
        {
            case Type.Produce: return GetDescriptionProduce();
            case Type.ProduceUnit: return GetDescriptionProduceUnit();
            case Type.ConsumeProduce: return GetDescriptionConsumeProduce();
            case Type.Scavenger: return "Allows trading resources for new cards";
            case Type.Library: return "Allows researching the Malaise";
            case Type.Outpost: return "Extends the Building Reach area";

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
            case Type.Scavenger: return IconFactory.GetVisualsForScavengerEffect(this, Parent);
            case Type.Library: //fallthrough
            case Type.Outpost: return IconFactory.GetVisualsForOtherEffect(this, Parent);
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

}