using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Assertions;

public abstract class UnitData : ISaveable
{


    public int GetMovementCostTo(Location ToLocation)
    {
        List<Location> Path = Pathfinding.FindPathFromTo(Location, ToLocation);
        return Pathfinding.GetCostsForPath(Path);
    }

    public void MoveTo(Location Location, int Costs)
    {
        this.Location = Location;
        this.RemainingMovement -= Costs;

        if (!Game.TryGetService(out MapGenerator MapGenerator))
            return;

        if (!MapGenerator.TryGetHexagon(Location, out HexagonVisualization NewHex))
            return;

        NewHex.UpdateDiscoveryState(VisitingRange, ScoutingRange);
    }


    public void MoveAlong(List<Location> Path)
    {
        Assert.AreNotEqual(Path.Count, 0);
        Assert.AreEqual(Path[0], Location);
        for (int i = 1; i < Path.Count; i++)
        {
            Location CurrentLocation = this.Location;
            Location NextLocation = Path[i];
            int MoveCosts = HexagonConfig.GetCostsFromTo(CurrentLocation, NextLocation);
            if (RemainingMovement < MoveCosts)
                break;

            MoveTo(NextLocation, MoveCosts);
        }
    }

    public abstract Production GetFoodCosts();

    public abstract string GetPrefabName();

    public virtual int GetSize()
    {
        // 4 bytes, one each for MovementPerTurn, RemainingMovement, Visiting, ScoutingRange
        return sizeof(char) + 4 * sizeof(byte) + Location.GetStaticSize();
    }

    public virtual byte[] GetData()
    {
        NativeArray<byte> Bytes = new(GetSize(), Allocator.Temp);
        int Pos = 0;
        Pos = SaveGameManager.AddByte(Bytes, Pos, (byte)MovementPerTurn);
        Pos = SaveGameManager.AddByte(Bytes, Pos, (byte)RemainingMovement);
        Pos = SaveGameManager.AddByte(Bytes, Pos, (byte)VisitingRange);
        Pos = SaveGameManager.AddByte(Bytes, Pos, (byte)ScoutingRange);
        Pos = SaveGameManager.AddSaveable(Bytes, Pos, Location);

        return Bytes.ToArray();
    }

    public virtual void SetData(NativeArray<byte> Bytes)
    {
        int Pos = 0;
        Pos = SaveGameManager.GetByte(Bytes, Pos, out byte bMovementPerTurn);
        Pos = SaveGameManager.GetByte(Bytes, Pos, out byte bRemainingMovement);
        Pos = SaveGameManager.GetByte(Bytes, Pos, out byte bVisitingRange);
        Pos = SaveGameManager.GetByte(Bytes, Pos, out byte bScoutingRage);
        Pos = SaveGameManager.SetSaveable(Bytes, Pos, Location);

        MovementPerTurn = bMovementPerTurn;
        RemainingMovement = bRemainingMovement;
        VisitingRange = bVisitingRange;
        ScoutingRange = bScoutingRage;
    }

    public int MovementPerTurn = 1;
    public int RemainingMovement = 0;
    public Location Location;

    // should always be greater than the MovementPerTurn!
    public int VisitingRange = 2;
    public int ScoutingRange = 3;

    // don't save, its only temporarily constructed anyway
    public UnitVisualization Visualization;
}
