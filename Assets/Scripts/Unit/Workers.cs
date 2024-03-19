using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class Workers : GameService
{

    public bool RequestWorkerFor(BuildingData Building) {
        if (!HasUnemployedWorkers())
            return false;

        AmountOfEmployedWorkers++;
        if (BuildingsWithWorkers.Contains(Building))
            return true;

        BuildingsWithWorkers.Add(Building);

        return true;
    }

    public void ReleaseWorkerFrom(BuildingData Building) {
        if (!BuildingsWithWorkers.Contains(Building))
            return;

        AmountOfEmployedWorkers--;

        if (Building.WorkerCount == 0)
        {
            BuildingsWithWorkers.Remove(Building);
        }
    }

    public void ReleaseAndKillWorkerFrom(BuildingData Building)
    {
        ReleaseWorkerFrom(Building);
        KillWorker();
    }

    private void KillWorker()
    {
        AmountOfWorkers--;
    }

    private void KillWorkerFromRandomBuilding()
    {
        int RandomIndex = UnityEngine.Random.Range(0, BuildingsWithWorkers.Count);
        ReleaseAndKillWorkerFrom(BuildingsWithWorkers[RandomIndex]);
    }

    public int GetEmployedWorkerCount() {
        return AmountOfEmployedWorkers;
    }

    public int GetUnemployedWorkerCount() {
        return AmountOfWorkers - AmountOfEmployedWorkers;
    }

    public bool HasUnemployedWorkers()
    {
        return GetUnemployedWorkerCount() > 0;
    }

    public int GetTotalWorkerCount() {
        return AmountOfWorkers;
    }

    public void CreateNewWorker()
    {
        AmountOfWorkers++;
    }

    public Production GetWorkerCosts()
    {
        Production Cost = CostsPerWorker * GetTotalWorkerCount();

        return Cost;
    }

    public int HandleStarvation(int Food)
    {
        if (Food >= 0)
            return 0;

        // try to starve unassigned ones first, as this causes less hassle to the player
        int AmountToStarve = Mathf.Min(Mathf.Abs(Food), GetTotalWorkerCount());
        int AmountOfUnassignedToStarve = Mathf.Min(GetUnemployedWorkerCount(), AmountToStarve);
        int AmountOfAssignedToStarve = Mathf.Min(GetEmployedWorkerCount(), AmountToStarve - AmountOfUnassignedToStarve);

        // unassigned, so we don't need to update the building
        for (int i = 0; i < AmountOfUnassignedToStarve; i++)
        {
            KillWorker();
        }

        for (int i = 0; i < AmountOfAssignedToStarve; i++)
        {
            KillWorkerFromRandomBuilding();
        }

        MessageSystem.CreateMessage(Message.Type.Warning, AmountToStarve + " workers died of starvation!");
        return AmountToStarve;
    }

    protected override void StartServiceInternal() {}

    protected override void StopServiceInternal() {}

    public int AmountOfWorkers = 2;
    public int AmountOfEmployedWorkers = 0;
    public List<BuildingData> BuildingsWithWorkers = new();

    public static Production CostsPerWorker = new Production(Production.Type.Food, 1);
}
