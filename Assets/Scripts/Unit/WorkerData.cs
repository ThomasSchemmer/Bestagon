using System;
using System.Collections.Generic;
using System.Numerics;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Assertions;
using static UnityEngine.Rendering.DebugUI;

[Serializable]
public class WorkerData : ISaveable
{
    public WorkerData() {
        RemainingMovement = MovementPerTurn;
    }

    public int GetMovementCostTo(Location ToLocation) {
        List<Location> Path = Pathfinding.FindPathFromTo(Location, ToLocation);
        return Pathfinding.GetCostsForPath(Path);
    }

    public void MoveTo(Location Location, int Costs) {
        this.Location = Location;
        this.RemainingMovement -= Costs;
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
        // 2 bytes, one each for MovementPerTurn and RemainingMovement
        return MAX_NAME_LENGTH * sizeof(char) + 2 + Location.GetSize();
    }

    public byte[] GetData()
    {
        NativeArray<byte> Bytes = new(GetSize(), Allocator.Temp);
        int Pos = 0;
        Pos = SaveGameManager.AddString(Bytes, Pos, Name);
        Pos = SaveGameManager.AddByte(Bytes, Pos, (byte)MovementPerTurn);
        Pos = SaveGameManager.AddByte(Bytes, Pos, (byte)RemainingMovement);
        Pos = SaveGameManager.AddSaveable(Bytes, Pos, Location);

        return Bytes.ToArray();
    }

    public void SetData(byte[] Dsata)
    {
        throw new NotImplementedException();
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
    public int MovementPerTurn = 3;
    public int RemainingMovement = 0;
    public Location Location;

    public BuildingData AssignedBuilding;
    public WorkerVisualization Visualization;

    public static int MAX_NAME_LENGTH = 10;
}
