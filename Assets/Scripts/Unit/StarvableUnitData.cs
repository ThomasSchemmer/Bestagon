using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

/** A unit that can be starved. If hungry will not work */
public abstract class StarvableUnitData : UnitData
{
    public void HandleStarvation()
    {
        CurrentFoodCount = Mathf.Max(CurrentFoodCount - 1, 0);
    }

    public bool IsStarving()
    {
        return CurrentFoodCount == 0;
    }

    public void HandleFeeding(Production Food)
    {
        int MinFoodIndex = (int)Production.GoodsType.Food;
        int MaxFoodIndex = (int)Production.GoodsType.LuxuryItems - 1;
        for (int i = MaxFoodIndex; i >= MinFoodIndex; i--)
        {
            Production.Type FoodType = (Production.Type)i;
            if (Food[FoodType] == 0)
                continue;

            Food[FoodType] -= 1;
            CurrentFoodCount += Production.GetHungerFromFood(FoodType);
        }
    }

    public static void HandleStarvationFor<T>(List<T> Units, Production Production, string Name) where T : StarvableUnitData
    {
        int StarvingCount = 0;
        foreach (StarvableUnitData Unit in Units)
        {
            Unit.HandleStarvation();
            if (!Unit.IsStarving())
                continue;

            Unit.HandleFeeding(Production);
            StarvingCount += Unit.IsStarving() ? 1 : 0;
        }

        if (StarvingCount == 0)
            return;

        MessageSystem.CreateMessage(Message.Type.Warning, StarvingCount + " "+ Name+" are starving - they will not work!");
    }

    public override int GetSize()
    {
        return GetStaticSize();
    }

    public static int GetStaticSize()
    {
        // type and foodcount
        return sizeof(byte) + sizeof(int);
    }

    public override byte[] GetData()
    {
        NativeArray<byte> Bytes = new(GetSize(), Allocator.Temp);
        int Pos = 0;
        Pos = SaveGameManager.AddEnumAsByte(Bytes, Pos, (byte)Type);
        Pos = SaveGameManager.AddInt(Bytes, Pos, CurrentFoodCount);

        return Bytes.ToArray();
    }

    public override void SetData(NativeArray<byte> Bytes)
    {
        int Pos = 0;
        Pos = SaveGameManager.GetEnumAsByte(Bytes, Pos, out byte bType);
        Pos = SaveGameManager.GetInt(Bytes, Pos, out CurrentFoodCount);

        Type = (UnitType)bType;
    }

    [HideInInspector]
    public int CurrentFoodCount = 1;
}
