using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;

/** 
 * Workers can be assigned in buildings to generate resources
 * Not visualized apart from indicators!
 */
[CreateAssetMenu(fileName = "Worker", menuName = "ScriptableObjects/Worker", order = 5)]
public class WorkerEntity : StarvableUnitEntity
{
    // will be queried on save/load
    private BuildingEntity AssignedBuilding = null;
    [SaveableBaseType]
    private int AssignedBuildingSlot = -1;
    [SaveableClass]
    private Location AssignedLocation = null;

    public bool IsEmployed()
    {
        return AssignedBuilding != null && AssignedBuildingSlot != -1;
    }

    public void AssignToBuilding(BuildingEntity Building, int i)
    {
        AssignedBuilding = Building;
        AssignedBuildingSlot = i;
        AssignedLocation = Building.GetLocations().GetMainLocation();
    }

    public void RemoveFromBuilding()
    {
        AssignedBuilding = null;
        AssignedBuildingSlot = -1;
        AssignedLocation = null;
    }

    public BuildingEntity GetAssignedBuilding()
    {
        return AssignedBuilding;
    }

    public int GetAssignedBuildingSlot()
    {
        return AssignedBuildingSlot;
    }

    protected override bool IsInFoodProductionBuilding()
    {
        BuildingEntity AssignedBuilding = GetAssignedBuilding();
        if (AssignedBuilding == null)
            return false;

        return AssignedBuilding.IsFoodProductionBuilding();
    }

    public void OnLoaded()
    {
        if (AssignedBuildingSlot == -1)
            return;

        Game.RunAfterServiceInit((BuildingService Buildings) =>
        {
            if (!Buildings.TryGetEntityAt(AssignedLocation, out BuildingEntity Building))
                return;

            Game.RunAfterServiceInit((Workers Workers) =>
            {
                Workers.AssignWorkerTo(this, Building, AssignedBuildingSlot);
            });
        });
    }

    protected override int GetFoodConsumption()
    {
        AttributeSet Attributes = AttributeSet.Get();
        return (int)Attributes[AttributeType.WorkerFoodConsumption].CurrentValue;
    }

    public override bool TryInteractWith(HexagonVisualization Hex)
    {
        if (!Game.TryGetServices(out Workers Workers, out Stockpile Stockpile))
            return false;

        Init();
        Workers.AddWorker(this);
        Stockpile.RequestUIRefresh();
        return true;
    }

    public override bool IsAboutToBeMalaised()
    {
        var Building = GetAssignedBuilding();
        if (Building == null)
            return false;

        return Building.IsAboutToBeMalaised();
    }

    public override bool IsIdle()
    {
        return GetAssignedBuilding() == null;
    }

    public override int GetTargetMeshIndex()
    {
        return -1;
    }
}
