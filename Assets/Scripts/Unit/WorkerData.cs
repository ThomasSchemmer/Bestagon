using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public class WorkerData : StarvableUnitData, ISaveable
{
    private BuildingData AssignedBuilding = null;
    private int AssignedBuildingSlot = -1;

    public bool IsEmployed()
    {
        return AssignedBuilding != null && AssignedBuildingSlot != -1;
    }

    public void AssignToBuilding(BuildingData Building, int i)
    {
        AssignedBuilding = Building;
        AssignedBuildingSlot = i;
    }

    public void RemoveFromBuilding()
    {
        AssignedBuilding = null;
        AssignedBuildingSlot = -1;
    }

    public BuildingData GetAssignedBuilding()
    {
        return AssignedBuilding;
    }

    public int GetAssignedBuildingSlot()
    {
        return AssignedBuildingSlot;
    }

    public override int GetSize()
    {
        // foodcount, employed, assigned building slot
        return base.GetSize() + sizeof(byte) * 3 + Location.GetStaticSize();
    }

    public override byte[] GetData()
    {
        NativeArray<byte> Bytes = SaveGameManager.GetArrayWithBaseFilled(this, GetSize(), base.GetData());

        int Pos = base.GetSize();
        bool bIsEmployed = AssignedBuilding != null;
        Pos = SaveGameManager.AddByte(Bytes, Pos, (byte)FoodCount);
        Pos = SaveGameManager.AddBool(Bytes, Pos, bIsEmployed);
        Pos = SaveGameManager.AddByte(Bytes, Pos, (byte)AssignedBuildingSlot);
        Pos = SaveGameManager.AddSaveable(Bytes, Pos, bIsEmployed ? AssignedBuilding.Location : Location.Zero);

        return Bytes.ToArray();
    }

    public override void SetData(NativeArray<byte> Bytes)
    {
        base.SetData(Bytes);
        if (!Game.TryGetServices(out MapGenerator MapGen, out Workers Workers))
            return;

        int Pos = base.GetSize();
        Location Location = Location.Zero;
        Pos = SaveGameManager.GetByte(Bytes, Pos, out byte bFoodCount);
        Pos = SaveGameManager.GetBool(Bytes, Pos, out bool bIsEmployed);
        Pos = SaveGameManager.GetByte(Bytes, Pos, out byte bAssignedBuildingSlot);
        Pos = SaveGameManager.SetSaveable(Bytes, Pos, Location);

        FoodCount = bFoodCount;
        if (!bIsEmployed)
            return;

        if (!MapGen.TryGetBuildingAt(Location, out BuildingData Building))
            return;

        Workers.AssignWorkerTo(this, Building, bAssignedBuildingSlot);
    }
}
