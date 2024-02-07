using System;
using System.Collections.Generic;
using System.Numerics;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Assertions;

[Serializable]
public class WorkerData : ISaveable
{
    public WorkerData() {
        RemainingMovement = MovementPerTurn;
        ID = CURRENT_WORKER_ID++;
    }

    public int GetMovementCostTo(Location ToLocation) {
        List<Location> Path = Pathfinding.FindPathFromTo(Location, ToLocation);
        return Pathfinding.GetCostsForPath(Path);
    }

    public void MoveTo(Location Location, int Costs) {
        this.Location = Location;
        this.RemainingMovement -= Costs;

        if (!Game.TryGetService(out MapGenerator MapGenerator))
            return;

        if (!MapGenerator.TryGetHexagon(Location, out HexagonVisualization NewHex))
            return;

        NewHex.UpdateDiscoveryState(VisitingRange, ScoutingRange);
    }

    public void MoveAlong(List<Location> Path) {
        Assert.AreNotEqual(Path.Count, 0);
        Assert.AreEqual(Path[0], Location);
        for (int i = 1; i < Path.Count; i++) {
            Location CurrentLocation = this.Location;
            Location NextLocation = Path[i];
            int MoveCosts = HexagonConfig.GetCostsFromTo(CurrentLocation, NextLocation);
            if (RemainingMovement < MoveCosts)
                break;

            MoveTo(NextLocation, MoveCosts);
        }
    }

    public void RemoveFromBuilding() {
        if (AssignedBuilding == null)
            return;

        AssignedBuilding.RemoveWorker(this);
        AssignedBuilding = null;
    }

    public int GetSize()
    {
        // 4 bytes, one each for MovementPerTurn, RemainingMovement, Visiting and ScoutingRange
        return MAX_NAME_LENGTH * sizeof(char) + 4 * sizeof(byte) + Location.GetStaticSize();
    }

    public byte[] GetData()
    {
        NativeArray<byte> Bytes = new(GetSize(), Allocator.Temp);
        int Pos = 0;
        Pos = SaveGameManager.AddString(Bytes, Pos, Name);
        Pos = SaveGameManager.AddInt(Bytes, Pos, ID);
        Pos = SaveGameManager.AddByte(Bytes, Pos, (byte)MovementPerTurn);
        Pos = SaveGameManager.AddByte(Bytes, Pos, (byte)RemainingMovement);
        Pos = SaveGameManager.AddByte(Bytes, Pos, (byte)VisitingRange);
        Pos = SaveGameManager.AddByte(Bytes, Pos, (byte)ScoutingRange);
        Pos = SaveGameManager.AddSaveable(Bytes, Pos, Location);

        return Bytes.ToArray();
    }

    public void SetData(NativeArray<byte> Bytes)
    {
        int Pos = 0;
        Pos = SaveGameManager.GetString(Bytes, Pos, MAX_NAME_LENGTH, out Name);
        Pos = SaveGameManager.GetInt(Bytes, Pos, out ID);
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

    public string GetName()
    {
        return Name.Replace(" ", "");
    }

    public void SetName(string NewName)
    {
        if (NewName.Length > MAX_NAME_LENGTH)
        {
            NewName = NewName[..MAX_NAME_LENGTH];
        }
        for (int i = NewName.Length; i < MAX_NAME_LENGTH; i++)
        {
            NewName = " " + NewName;
        }
        Name = NewName;
    }

    public string Name;
    public int MovementPerTurn = 1;
    public int RemainingMovement = 0;
    public Location Location;
    public int ID = 0;
    // should always be greater than the MovementPerTurn!
    public int VisitingRange = 2;
    public int ScoutingRange = 3;

    //save as bool and location!
    public BuildingData AssignedBuilding;
    public WorkerVisualization Visualization;

    public static int MAX_NAME_LENGTH = 10;
    public static int CURRENT_WORKER_ID = 0;
}
