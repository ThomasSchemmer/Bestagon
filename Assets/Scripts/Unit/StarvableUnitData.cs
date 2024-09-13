using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

/** A unit that can be starved. If hungry will not work */
public abstract class StarvableUnitData : UnitData
{
    public void HandleStarvation(bool bIsSimulated)
    {
        int FoodConsumption = GetFoodConsumption();
        if (bIsSimulated)
        {
            SimulatedFoodCount = Mathf.Max(SimulatedFoodCount - FoodConsumption, 0);
        }
        else
        {
            CurrentFoodCount = Mathf.Max(CurrentFoodCount - FoodConsumption, 0);
        }
    }

    public bool IsStarving(bool bIsSimulated)
    {
        return (bIsSimulated ? SimulatedFoodCount : CurrentFoodCount) == 0;
    }

    public bool IsReadyToWork(bool bIsSimulated)
    {
        return !IsStarving(bIsSimulated) || IsInFoodProductionBuilding();
    }

    protected abstract int GetFoodConsumption();

    public void HandleFeeding(Production Food, bool bIsSimulated)
    {
        int MinFoodIndex = (int)Production.GoodsType.Food;
        int MaxFoodIndex = (int)Production.GoodsType.LuxuryItems - 1;
        int FoodConsumption = GetFoodConsumption();
        // todo: make optimal instead of greedy
        for (int i = MaxFoodIndex; i >= MinFoodIndex; i--)
        {
            Production.Type FoodType = (Production.Type)i;
            int Nutrition = Production.GetNutrition(FoodType);
            int AvailableNutrition = Food[FoodType] * Nutrition;
            if (AvailableNutrition == 0)
                continue;

            int WantedFoodAmount = Mathf.CeilToInt((float)FoodConsumption / Nutrition);
            int ActualFoodAmount = Mathf.Min(WantedFoodAmount, Food[FoodType]);
            int ActualNutrition = ActualFoodAmount * Nutrition;

            Food[FoodType] -= ActualFoodAmount;
            FoodConsumption -= ActualNutrition;
            if (bIsSimulated)
            {
                SimulatedFoodCount += ActualNutrition;
            }
            else
            {
                CurrentFoodCount += ActualNutrition;
            }
        }
    }

    public static void HandleStarvationFor<T>(List<T> Units, Production Production, string Name, bool bIsSimulated) where T : StarvableUnitData
    {
        int StarvingCount = 0;
        foreach (StarvableUnitData Unit in Units)
        {
            Unit.HandleStarvation(bIsSimulated);
            if (!Unit.IsStarving(bIsSimulated))
                continue;

            Unit.HandleFeeding(Production, bIsSimulated);
            StarvingCount += Unit.IsStarving(bIsSimulated) ? 1 : 0;
        }

        if (bIsSimulated)
            return;

        if (StarvingCount == 0)
            return;

        MessageSystemScreen.CreateMessage(Message.Type.Warning, StarvingCount + " "+ Name+" are starving - they will not work!");
    }

    protected virtual bool IsInFoodProductionBuilding()
    {
        // overriden in subclasses
        return false;
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
        NativeArray<byte> Bytes = new(GetStaticSize(), Allocator.Temp);
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
    [HideInInspector]
    public int SimulatedFoodCount = 1;
}
