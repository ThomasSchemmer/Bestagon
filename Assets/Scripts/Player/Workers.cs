using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Workers : MonoBehaviour
{
    public void Start() {
        for (int i = 0; i < MaxWorker; i++) {
            UnassignedWorker.Add(new Worker() {
                Name = "Worker " + i
            });
        }    
    }

    public static Production GetWorkerCosts() {
        return CostsPerWorker * GetTotalWorkerCount();
    }

    public static Worker GetWorker() {
        if (UnassignedWorker.Count == 0) {
            MessageSystem.CreateMessage(Message.Type.Error, "No idle worker available!");
            return null;
        }

        Worker Worker = UnassignedWorker[0];
        AssignedWorker.Add(Worker);
        UnassignedWorker.RemoveAt(0);
        return Worker;
    }

    public static void ReturnWorker(Worker Worker, BuildingData B) {
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
        UnassignedWorker.RemoveRange(0, AmountOfUnassignedToStarve);

        // get the building of the worker to remove the cross-references
        for (int i = 0; i < AmountOfAssignedToStarve; i++) {
            Worker WorkerToStarve = AssignedWorker[0];
            WorkerToStarve.AssignedBuilding.RemoveWorker(WorkerToStarve);
            AssignedWorker.RemoveAt(0);
        }

        MessageSystem.CreateMessage(Message.Type.Warning, AmountToStarve + " workers died of starvation!");
    }

    public static List<Worker> UnassignedWorker = new List<Worker>();
    public static List<Worker> AssignedWorker = new List<Worker>();

    public static int MaxWorker = 10;
    public static Production CostsPerWorker = new Production(0, 0, 0, 1);
}
