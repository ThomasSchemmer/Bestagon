using System;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine.Assertions;

[Serializable]
public class WorkerData
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

    public string Name = "";
    public int MovementPerTurn = 3;
    public int RemainingMovement = 0; 
    public Location Location;

    public BuildingData AssignedBuilding;
    public WorkerVisualization Visualization;
}
