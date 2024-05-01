using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public class Unlockables : GameService, ISaveable
{
    public BuildingConfig.Type[] LockedTypesPerCategory;

    public bool TryUnlockNewBuildingType(out BuildingConfig.Type Type, bool bIsPreview = false)
    {
        Type = BuildingConfig.Type.Mine;
        if (!IsInit)
            return false;

        for (int CategoryIndex = 0; CategoryIndex < LockedTypesPerCategory.Length; CategoryIndex++)
        {
            int Category = (int)LockedTypesPerCategory[CategoryIndex];
            if (IsCategoryFullyUnlocked(Category))
                continue;

            //todo: remove after adding all buildings
            Type = GetRandomTypeFromMask(Category);
            Type = BuildingConfig.Type.HerbalistsHut;
            if (!bIsPreview)
            {
                MarkAsUnlocked(Type, CategoryIndex);
            }
            return true;
        }

        return false;
    }

    public void UnlockSpecificBuildingType(BuildingConfig.Type Type)
    {
        // we can simply always set this to "unlocked" as its only tracking locked types per category
        for (int i = 0; i < LockedTypesPerCategory.Length; i++)
        {
            MarkAsUnlocked(Type, i);
        }
    }

    private void MarkAsUnlocked(BuildingConfig.Type Type, int CategoryIndex) 
    {
        LockedTypesPerCategory[CategoryIndex] = (BuildingConfig.Type)((int)LockedTypesPerCategory[CategoryIndex] & (~(int)Type));
    }

    public BuildingConfig.Type GetRandomUnlockedType()
    {
        // this implies that a category can only be unlocked if all previous things have been unlocked!
        int UnlockedCategories = 0;
        for (int i = 0; i < BuildingConfig.CategoryAmount; i++)
        {
            UnlockedCategories += LockedTypesPerCategory[i] != BuildingConfig.Categories[i] ? 1 : 0;
        }

        int RandomCategory = UnityEngine.Random.Range(0, UnlockedCategories);
        int UnlockedTypes = ~(int)LockedTypesPerCategory[RandomCategory] & (int)BuildingConfig.Categories[RandomCategory];

        return GetRandomTypeFromMask(UnlockedTypes);
    }

    private bool IsCategoryFullyUnlocked(int Category)
    {
        return HexagonConfig.GetSetBitsAmount(Category) == 0;
    }

    private BuildingConfig.Type GetRandomTypeFromMask(int Mask)
    {
        int BitMaxAmount = HexagonConfig.GetSetBitsAmount(Mask);
        int RandomIndex = UnityEngine.Random.Range(0, BitMaxAmount);
        int SelectedBit = -1;

        for (int i = 0; i < 32 && SelectedBit < BitMaxAmount; i++)
        {
            int Bit = (Mask & (1 << i)) >> i;
            SelectedBit += Bit;
            if (SelectedBit == RandomIndex && Bit > 0)
                return (BuildingConfig.Type)(1 << i);
        }

        return BuildingConfig.Type.DEFAULT;
    }

    private void InitializeCategories()
    {
        LockedTypesPerCategory = new BuildingConfig.Type[BuildingConfig.CategoryAmount];
        LockedTypesPerCategory[0] = BuildingConfig.CategoryMeadow;
        LockedTypesPerCategory[1] = BuildingConfig.CategoryDesert;
        LockedTypesPerCategory[2] = BuildingConfig.CategorySwamp;
        LockedTypesPerCategory[3] = BuildingConfig.CategoryIce;

        UnlockSpecificBuildingType(BuildingConfig.UnlockOnStart);
    }

    public byte[] GetData()
    {
        NativeArray<byte> Bytes = new(GetSize(), Allocator.Temp);
        int Pos = 0;

        Pos = SaveGameManager.AddInt(Bytes, Pos, BuildingConfig.CategoryAmount);
        for (int i = 0; i < BuildingConfig.CategoryAmount; i++)
        {
            Pos = SaveGameManager.AddInt(Bytes, Pos, (int)LockedTypesPerCategory[0]);
        }

        return Bytes.ToArray();
    }

    public int GetSize()
    {
        return sizeof(int) * (BuildingConfig.CategoryAmount + 1);
    }

    public void SetData(NativeArray<byte> Bytes)
    {
        InitializeCategories();
        int Pos = sizeof(int);
        Pos = SaveGameManager.GetInt(Bytes, Pos, out int Count);
        for (int i = 0; i < Count; i++)
        {
            Pos = SaveGameManager.GetInt(Bytes, Pos, out int Temp);
            LockedTypesPerCategory[i] = (BuildingConfig.Type)Temp;
        }
    }

    protected override void StartServiceInternal() {
        Game.RunAfterServiceInit((SaveGameManager SaveGameManager) =>
        {
            if (!SaveGameManager.HasDataFor(ISaveable.SaveGameType.Unlockables))
            {
                InitializeCategories();
            }

            _OnInit?.Invoke();
        });
    }

    protected override void StopServiceInternal() {}
}