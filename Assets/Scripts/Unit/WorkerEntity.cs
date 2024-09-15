using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

/** 
 * Workers can be assigned in buildings to generate resources
 * Not visualized apart from indicators!
 */
[CreateAssetMenu(fileName = "Worker", menuName = "ScriptableObjects/Worker", order = 5)]
public class WorkerEntity : StarvableUnitEntity, ISaveableData
{
    private BuildingEntity AssignedBuilding = null;
    private int AssignedBuildingSlot = -1;

    public bool IsEmployed()
    {
        return AssignedBuilding != null && AssignedBuildingSlot != -1;
    }

    public void AssignToBuilding(BuildingEntity Building, int i)
    {
        AssignedBuilding = Building;
        AssignedBuildingSlot = i;
    }

    public void RemoveFromBuilding()
    {
        AssignedBuilding = null;
        AssignedBuildingSlot = -1;
    }

    public BuildingEntity GetAssignedBuilding()
    {
        return AssignedBuilding;
    }

    public int GetAssignedBuildingSlot()
    {
        return AssignedBuildingSlot;
    }

    public override int GetSize()
    {
        return GetStaticSize();
    }

    protected override bool IsInFoodProductionBuilding()
    {
        BuildingEntity AssignedBuilding = GetAssignedBuilding();
        if (AssignedBuilding == null)
            return false;

        return AssignedBuilding.IsFoodProductionBuilding();
    }

    public static new int GetStaticSize()
    {
        // foodcount, employed, assigned building slot
        return StarvableUnitEntity.GetStaticSize() + sizeof(byte) * 3 + Location.GetStaticSize();
    }

    public override byte[] GetData()
    {
        NativeArray<byte> Bytes = SaveGameManager.GetArrayWithBaseFilled(this, base.GetSize(), base.GetData());

        int Pos = base.GetSize();
        bool bIsEmployed = AssignedBuilding != null;
        Pos = SaveGameManager.AddByte(Bytes, Pos, (byte)CurrentFoodCount);
        Pos = SaveGameManager.AddBool(Bytes, Pos, bIsEmployed);
        Pos = SaveGameManager.AddByte(Bytes, Pos, (byte)AssignedBuildingSlot);
        Pos = SaveGameManager.AddSaveable(Bytes, Pos, bIsEmployed ? AssignedBuilding.GetLocation() : Location.Zero);

        return Bytes.ToArray();
    }

    public override void SetData(NativeArray<byte> Bytes)
    {
        base.SetData(Bytes);
        if (!Game.TryGetServices(out BuildingService Buildings, out Workers Workers))
            return;

        int Pos = base.GetSize();
        Location Location = Location.Zero;
        Pos = SaveGameManager.GetByte(Bytes, Pos, out byte bFoodCount);
        Pos = SaveGameManager.GetBool(Bytes, Pos, out bool bIsEmployed);
        Pos = SaveGameManager.GetByte(Bytes, Pos, out byte bAssignedBuildingSlot);
        Pos = SaveGameManager.SetSaveable(Bytes, Pos, Location);

        CurrentFoodCount = bFoodCount;
        if (!bIsEmployed)
            return;

        if (!Buildings.TryGetBuildingAt(Location, out BuildingEntity Building))
            return;

        Workers.AssignWorkerTo(this, Building, bAssignedBuildingSlot);
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
}
