using System;
using System.Collections.Generic;
using System.Numerics;
using Unity.Collections;
using Unity.Plastic.Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Assertions;

[Serializable]
public class WorkerData : UnitData
{
    public enum State
    {
        Normal,
        Depressed,
        Hermit
    }

    public WorkerData() {
        RemainingMovement = MovementPerTurn;
        ID = CURRENT_WORKER_ID++;
    }

    public void RemoveFromBuilding() {
        if (AssignedBuilding == null)
            return;

        AssignedBuilding.RemoveWorker(this);
        AssignedBuilding = null;
    }

    public override Production GetFoodCosts()
    {
        return Workers.CostsPerWorker * GetCostModifierByState();
    }

    public override string GetPrefabName()
    {
        return "Worker";
    }

    private int GetCostModifierByState()
    {
        return CurrentState == State.Depressed ? 2 : 1;
    }

    public void UpdateFamilyState(int DistanceToOthers)
    {
        State OldState = CurrentState;
        DistanceToClosestWorker = DistanceToOthers;
        CurrentState =
            DistanceToClosestWorker > HermitRange ? State.Hermit :
            DistanceToClosestWorker > SocialRange ? State.Depressed :
            State.Normal;

        if (CurrentState == OldState)
            return;

        if (CurrentState == State.Hermit)
        {

        }

    }


    public override int GetSize()
    {
        // 2 bytes, one each for SocialRange, HermitRange
        return base.GetSize() + MAX_NAME_LENGTH * sizeof(char) + 2 * sizeof(byte);
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
        Pos = SaveGameManager.AddByte(Bytes, Pos, (byte)SocialRange);
        Pos = SaveGameManager.AddByte(Bytes, Pos, (byte)HermitRange);

        return Bytes.ToArray();
    }

    public override void SetData(NativeArray<byte> Bytes)
    {
        base.SetData(Bytes);
        int Pos = base.GetSize();
        Pos = SaveGameManager.GetString(Bytes, Pos, MAX_NAME_LENGTH, out Name);
        Pos = SaveGameManager.GetInt(Bytes, Pos, out ID);
        Pos = SaveGameManager.GetByte(Bytes, Pos, out byte bSocialRange);
        Pos = SaveGameManager.GetByte(Bytes, Pos, out byte bHermitRange);

        SocialRange = bSocialRange;
        HermitRange = bHermitRange;
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
    public State CurrentState = State.Normal;

    public int SocialRange = 3;
    public int HermitRange = 6;

    private int DistanceToClosestWorker = int.MaxValue;

    //save as bool and location!
    public BuildingData AssignedBuilding;

    public static int MAX_NAME_LENGTH = 10;
    public static int CURRENT_WORKER_ID = 0;
}
