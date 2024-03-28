using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

[Serializable]
public class ScoutData : UnitData
{
    public ScoutData() {
        RemainingMovement = MovementPerTurn;
        ID = CURRENT_WORKER_ID++;
    }

    public override string GetPrefabName()
    {
        return "Scout";
    }

    public override int GetSize()
    {
        // 2 bytes, one each for SocialRange, HermitRange
        return base.GetSize() + MAX_NAME_LENGTH * sizeof(char) + sizeof(byte);
    }

    public override byte[] GetData()
    {
        int BaseSize = base.GetSize();
        NativeArray<byte> Bytes = new(GetSize(), Allocator.Temp);
        NativeSlice<byte> Slice = new NativeSlice<byte>(Bytes, 0, BaseSize);
        Slice.CopyFrom(base.GetData());

        int Pos = BaseSize;
        Pos = SaveGameManager.AddString(Bytes, Pos, Name);
        Pos = SaveGameManager.AddInt(Bytes, Pos, ID);
        Pos = SaveGameManager.AddByte(Bytes, Pos, (byte)ActiveRange);

        return Bytes.ToArray();
    }

    public override void SetData(NativeArray<byte> Bytes)
    {
        base.SetData(Bytes);
        int Pos = base.GetSize();
        Pos = SaveGameManager.GetString(Bytes, Pos, MAX_NAME_LENGTH, out Name);
        Pos = SaveGameManager.GetInt(Bytes, Pos, out ID);
        Pos = SaveGameManager.GetByte(Bytes, Pos, out byte bActiveRange);

        ActiveRange = bActiveRange;
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
    public int ID = 0;

    public int ActiveRange = 3;

    //save as bool and location!
    public BuildingData AssignedBuilding;

    public static int MAX_NAME_LENGTH = 10;
    public static int CURRENT_WORKER_ID = 0;
}
