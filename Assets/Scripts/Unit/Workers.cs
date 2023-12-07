using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class Workers : MonoBehaviour
{
    public void Start() {
        for (int i = 0; i < WorkerStartLocations.Count; i++) {
            UnassignedWorker.Add(new WorkerData() {
                Name = "Worker " + i,
                Location = WorkerStartLocations[i]
            });
        }    
    }

    public static Production GetWorkerCosts() {
        return CostsPerWorker * GetTotalWorkerCount();
    }

    public static WorkerData GetWorker() {
        if (UnassignedWorker.Count == 0) {
            MessageSystem.CreateMessage(Message.Type.Error, "No idle worker available!");
            return null;
        }

        WorkerData Worker = UnassignedWorker[0];
        AssignedWorker.Add(Worker);
        UnassignedWorker.RemoveAt(0);
        return Worker;
    }

    public static void MakeWorkerWork(WorkerData Worker) {
        if (!UnassignedWorker.Contains(Worker)) {
            MessageSystem.CreateMessage(Message.Type.Error, "Worker is already working, should be unassigned!");
            return;
        }

        AssignedWorker.Add(Worker);
        UnassignedWorker.Remove(Worker);
    }

    public static void ReturnWorker(WorkerData Worker) {
        if (Worker == null)
            return;

        AssignedWorker.Remove(Worker);
        UnassignedWorker.Add(Worker);
    }

    public static int GetAssignedWorkerCount() {
        return AssignedWorker.Count;
    }

    public static int GetUnassignedWorkerCount() {
        return UnassignedWorker.Count;
    }

    public static int GetTotalWorkerCount() {
        return GetUnassignedWorkerCount() + GetAssignedWorkerCount();
    }

    public static void Starve(int Food) {
        if (Food >= 0)
            return;

        // try to starve unassigned ones first, as this causes less hassle to the player
        int AmountToStarve = Mathf.Min(Mathf.Abs(Food), GetTotalWorkerCount());
        int AmountOfUnassignedToStarve = Mathf.Min(UnassignedWorker.Count, AmountToStarve); 
        int AmountOfAssignedToStarve = Mathf.Min(AssignedWorker.Count, AmountToStarve - AmountOfUnassignedToStarve);

        // unassigned, so we don't need to update the building
        for (int i = 0; i < AmountOfUnassignedToStarve; i++) {
            StarveWorker(UnassignedWorker[0]);
        }

        for (int i = 0; i < AmountOfAssignedToStarve; i++) {
            StarveWorker(AssignedWorker[0]);
        }

        MessageSystem.CreateMessage(Message.Type.Warning, AmountToStarve + " workers died of starvation!");
    }

    private static void StarveWorker(WorkerData Worker) {
        Worker.RemoveFromBuilding();
        RemoveWorker(Worker);

        if (!Game.TryGetService(out MapGenerator MapGenerator))
            return;

        if (!MapGenerator.TryGetChunkData(Worker.Location, out ChunkData Chunk))
            return;

        if (!Chunk.Visualization)
            return;

        Chunk.Visualization.Refresh();
    }

    public static void RemoveWorker(WorkerData Worker) {
        UnassignedWorker.Remove(Worker);
        AssignedWorker.Remove(Worker);
    }

    public static List<WorkerData> GetWorkersInChunk(Location Location) {
        List<WorkerData> WorkersInChunk = new List<WorkerData>();
        foreach (WorkerData Worker in UnassignedWorker) {
            if (Worker.Location.ChunkLocation != Location.ChunkLocation)
                continue;

            WorkersInChunk.Add(Worker);
        }
        foreach (WorkerData Worker in AssignedWorker) {
            if (Worker.Location.ChunkLocation != Location.ChunkLocation)
                continue;

            WorkersInChunk.Add(Worker);
        }

        return WorkersInChunk;
    }

    public static bool TryGetWorkersAt(Location Location, out List<WorkerData> FoundWorker) {
        FoundWorker = new();
        foreach (WorkerData Worker in UnassignedWorker) {
            if (Worker.Location.Equals(Location)) {
                FoundWorker.Add(Worker);
            }
        }

        foreach (WorkerData Worker in AssignedWorker) {
            if (Worker.Location.Equals(Location)) {
                FoundWorker.Add(Worker);
            }
        }
        return FoundWorker.Count > 0;
    }

    public static void HandleEndOfTurn() {
        MoveToBuildings();
        Refresh();
    }

    private static void MoveToBuildings() {
        foreach (WorkerData Worker in AssignedWorker) {
            Assert.IsNotNull(Worker.AssignedBuilding, "Worker with null-building in assigned list");
            if (Worker.Location.Equals(Worker.AssignedBuilding.Location))
                continue;

            List<Location> Path = Pathfinding.FindPathFromTo(Worker.Location, Worker.AssignedBuilding.Location);
            Worker.MoveAlong(Path);
            if (!Worker.Visualization)
                continue;

            Worker.Visualization.UpdateLocation();
        }
    }

    private static void Refresh() {
        foreach (WorkerData Worker in UnassignedWorker) {
            Worker.RemainingMovement = Worker.MovementPerTurn;
        }

        foreach (WorkerData Worker in AssignedWorker) {
            Worker.RemainingMovement = Worker.MovementPerTurn;
        }
    }

    public static List<WorkerData> UnassignedWorker = new List<WorkerData>();
    public static List<WorkerData> AssignedWorker = new List<WorkerData>();

    public static Production CostsPerWorker = new Production(Production.Type.Food, 1);
    public static List<Location> WorkerStartLocations = new() {
        new Location(new Vector2Int(0, 0), new Vector2Int(1, 3)),
        new Location(new Vector2Int(0, 0), new Vector2Int(1, 4)),
        new Location(new Vector2Int(0, 0), new Vector2Int(2, 4)),
    };
}
