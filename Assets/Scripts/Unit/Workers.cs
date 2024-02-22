using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class Workers : GameService
{
    public WorkerData GetWorker() {
        if (UnassignedWorker.Count == 0) {
            MessageSystem.CreateMessage(Message.Type.Error, "No idle worker available!");
            return null;
        }

        WorkerData Worker = UnassignedWorker[0];
        AssignedWorker.Add(Worker);
        UnassignedWorker.RemoveAt(0);
        return Worker;
    }

    public WorkerData GetWorkerByID(int ID)
    {
        foreach (WorkerData Worker in UnassignedWorker)
        {
            if (Worker.ID == ID)
                return Worker;
        }
        foreach (WorkerData Worker in AssignedWorker)
        {
            if (Worker.ID == ID)
                return Worker;
        }
        return null;
    }

    public void MakeWorkerWork(WorkerData Worker) {
        if (!UnassignedWorker.Contains(Worker)) {
            MessageSystem.CreateMessage(Message.Type.Error, "Worker is already working, should be unassigned!");
            return;
        }

        AssignedWorker.Add(Worker);
        UnassignedWorker.Remove(Worker);
    }

    public void ReturnWorker(WorkerData Worker) {
        if (Worker == null)
            return;

        AssignedWorker.Remove(Worker);
        UnassignedWorker.Add(Worker);
    }

    public int GetAssignedWorkerCount() {
        return AssignedWorker.Count;
    }

    public int GetUnassignedWorkerCount() {
        return UnassignedWorker.Count;
    }

    public int GetTotalWorkerCount() {
        return GetUnassignedWorkerCount() + GetAssignedWorkerCount();
    }

    public void HandleStarvation(int Food) {
        if (Food >= 0)
            return;

        // try to starve unassigned ones first, as this causes less hassle to the player
        int AmountToStarve = Mathf.Min(Mathf.Abs(Food), GetTotalWorkerCount());
        int AmountOfUnassignedToStarve = Mathf.Min(UnassignedWorker.Count, AmountToStarve); 
        int AmountOfAssignedToStarve = Mathf.Min(AssignedWorker.Count, AmountToStarve - AmountOfUnassignedToStarve);

        // unassigned, so we don't need to update the building
        for (int i = 0; i < AmountOfUnassignedToStarve; i++) {
            DestroyWorker(UnassignedWorker[0]);
        }

        for (int i = 0; i < AmountOfAssignedToStarve; i++) {
            DestroyWorker(AssignedWorker[0]);
        }

        MessageSystem.CreateMessage(Message.Type.Warning, AmountToStarve + " workers died of starvation!");
    }

    public Production GetWorkerCosts()
    {
        Production Cost = new Production();
        foreach (WorkerData Worker in GetAllWorkers())
        {
            Cost += Worker.GetFoodCosts();
        }

        return Cost;
    }

    private void UpdateWorkerFamily()
    {
        List<WorkerData> AllWorkers = GetAllWorkers();
        List<WorkerData> WorkersToDelete = new();

        // might be better to do in a quadtree later. Can probably be mirrored
        for (int i = 0; i < AllWorkers.Count; i++)
        {
            int MinDistance = int.MaxValue;
            for (int j = 0; j < AllWorkers.Count; j++)
            {
                if (i == j)
                    continue;

                List<Location> Path = Pathfinding.FindPathFromTo(AllWorkers[i].Location, AllWorkers[j].Location);
                if (Path.Count < MinDistance)
                {
                    MinDistance = Path.Count;
                }
            }
            AllWorkers[i].UpdateFamilyState(MinDistance);
        }
    }

    public void DestroyWorker(WorkerData Worker)
    {
        Worker.RemoveFromBuilding();
        UnassignedWorker.Remove(Worker);
        AssignedWorker.Remove(Worker);

        if (UnassignedWorker.Count + AssignedWorker.Count <= 0)
        {
            Game.Instance.GameOver("Your tribe has died out!");
        }

        if (!Game.TryGetService(out MapGenerator MapGenerator))
            return;

        if (!MapGenerator.TryGetChunkData(Worker.Location, out ChunkData Chunk))
            return;

        if (!Chunk.Visualization)
            return;

        Chunk.Visualization.RefreshTokens();
    }

    public List<WorkerData> GetWorkersInChunk(Location Location) {
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

    public bool TryGetWorkersAt(Location Location, out List<WorkerData> FoundWorker) {
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

    public List<WorkerData> GetAllWorkers()
    {
        List<WorkerData> AllWorkers = new List<WorkerData>(GetTotalWorkerCount());
        AllWorkers.AddRange(UnassignedWorker);
        AllWorkers.AddRange(AssignedWorker);
        return AllWorkers;
    }

    public void HandleEndOfTurn() {
        MoveToBuildings();
        Refresh();
    }

    private void MoveToBuildings() {
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

    private void Refresh() {
        foreach (WorkerData Worker in UnassignedWorker) {
            Worker.RemainingMovement = Worker.MovementPerTurn;
        }

        foreach (WorkerData Worker in AssignedWorker) {
            Worker.RemainingMovement = Worker.MovementPerTurn;
        }

        UpdateWorkerFamily();
    }

    protected override void StartServiceInternal()
    {
        Game.RunAfterServiceInit((MapGenerator MapGenerator) =>
        {
            for (int i = 0; i < WorkerStartLocations.Count; i++)
            {
                WorkerData Worker = new WorkerData();
                Worker.SetName("Worker " + i);
                Worker.MoveTo(WorkerStartLocations[i], 0);
                UnassignedWorker.Add(Worker);
            }
            _OnInit?.Invoke();
        });
    }

    protected override void StopServiceInternal() {}

    public List<WorkerData> UnassignedWorker = new List<WorkerData>();
    public List<WorkerData> AssignedWorker = new List<WorkerData>();

    public static Production CostsPerWorker = new Production(Production.Type.Food, 1);
    public static List<Location> WorkerStartLocations = new() {
        new Location(new Vector2Int(0, 0), new Vector2Int(1, 4)),
        new Location(new Vector2Int(0, 0), new Vector2Int(1, 5)),
        new Location(new Vector2Int(0, 0), new Vector2Int(2, 4)),
    };
}
