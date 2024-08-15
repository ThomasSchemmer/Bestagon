using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/** Quest to potentially spawn a new Malaise origin */
public class MalaisedBuildingsQuest : Quest<BuildingData>
{
    public MalaisedBuildingsQuest() : base(){}

    public override int CheckSuccess(BuildingData Item)
    {
        return 3;
    }

    public override Dictionary<IQuestRegister<BuildingData>, ActionList<BuildingData>> GetRegisterMap()
    {
        return new()
        {
            { Game.GetService<BuildingService>(), BuildingService._OnBuildingDestroyed }
        };
    }

    public override string GetDescription()
    {
        return "Do not let the malaise destroy too many buildings!";
    }

    public override int GetMaxProgress()
    {
        return 1;
    }

    public override Type GetQuestType()
    {
        return Type.Negative;
    }

    public override Sprite GetSprite()
    {
        if (!Game.TryGetService(out IconFactory IconFactory))
            return null;

        return IconFactory.GetIconForMisc(IconFactory.MiscellaneousType.Buildings);
    }

    public override int GetStartProgress()
    {
        return 0;
    }

    public override void OnAfterCompletion()
    {

        if (!Game.TryGetServices(out Units Units, out MapGenerator MapGen))
            return;

        if (!Game.TryGetService(out Turn Turn))
            return;

        if (!Units.HasAnyUnit(UnitData.UnitType.Scout, out TokenizedUnitData Scout))
            return;

        int Distance = 6;
        HashSet<Location> Locations = Pathfinding.FindReachableLocationsFrom(Scout.Location, Distance);
        HexagonData Target = null;
        foreach (Location Location in Locations)
        {
            var Path = Pathfinding.FindPathFromTo(Scout.Location, Location);
            if (Path.Count < Distance - 1)
                continue;

            if (!MapGen.TryGetHexagonData(Location, out HexagonData Hex))
                continue;

            if (Hex.IsMalaised())
                continue;

            Target = Hex;
            break;
        }
        if (Target == null)
            return;

        var Neighbours = MapGen.GetNeighboursData(Target.Location, true);
        foreach (var Neighbour in Neighbours)
        {
            Neighbour.UpdateDiscoveryState(HexagonData.DiscoveryState.Visited);
        }

        MalaisedSpawnQuest.TargetLocation = Target.Location;
        MalaisedSpawnQuest.StartTurnNr = Turn.TurnNr;

        MessageSystemScreen.CreateMessage(Message.Type.Error, "Your scouts learned of a new emerging malaise cloud! Reach it before it spreads");
    }

    public override bool AreRequirementsFulfilled()
    {
        if (!Game.TryGetService(out Units Units))
            return false;

        return Units.HasAnyUnit(UnitData.UnitType.Scout, out TokenizedUnitData _);
    }

    public override bool TryGetNextType(out System.Type Type)
    {
        Type = typeof(MalaisedSpawnQuest);
        return true;
    }
    public override void OnCreated() { }

    public override void GrantRewards(){}
}
