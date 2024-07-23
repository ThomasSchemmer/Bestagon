using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using static HexagonData;

public class Unlockables : GameService, ISaveableService, IQuestRegister<BuildingConfig.Type>
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

            Type = GetRandomTypeFromMask(Category);

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
        BuildingConfig.Type OldMask = LockedTypesPerCategory[CategoryIndex];
        LockedTypesPerCategory[CategoryIndex] = (BuildingConfig.Type)((int)LockedTypesPerCategory[CategoryIndex] & (~(int)Type));

        BuildingConfig.Type NewMask = LockedTypesPerCategory[CategoryIndex];
        if (OldMask == NewMask)
            return;

        _OnUnlock.ForEach(_ => _.Invoke(Type));
    }

    private bool IsIndexInCategory(int Category, int Index)
    {
        return ((Category >> Index) & 0x1) == 1;
    }

    private bool IsIndexLockedInCategoryIndex(int CategoryIndex, int Index)
    { 
        return (((int)LockedTypesPerCategory[CategoryIndex] >> Index) & 0x1) == 1;
    }

    public bool IsLocked(BuildingConfig.Type Type)
    {
        int Wanted = HexagonConfig.MaskToInt((int)Type, 32);
        for (int i = 0; i < BuildingConfig.CategoryAmount; i++)
        {
            int Category = (int)BuildingConfig.Categories[i];
            if (!IsIndexInCategory(Category, Wanted))
                continue;

            return IsIndexLockedInCategoryIndex(i, Wanted);
        }
        return false;
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

    public bool TryGetRandomUnlockedResource(out Production.Type RandomType)
    {
        RandomType = default;
        if (!IsFullyLoaded())
            return false;

        if (!Game.TryGetService(out MeshFactory MeshFactory))
            return false;

        Production UnlockedCost = new();
        for (int i = 0; i < BuildingConfig.CategoryAmount; i++)
        {
            for (int j = 0; j < BuildingConfig.MaxIndex; j++) {
                int Category = (int)BuildingConfig.Categories[i];
                BuildingConfig.Type Type = (BuildingConfig.Type)(1 << j);
                if (!IsIndexInCategory(Category, j))
                    continue;

                if (IsIndexLockedInCategoryIndex(i, j))
                    continue;

                UnlockedCost += MeshFactory.CreateDataFromType(Type).Cost;
            }
        }

        List<Production.Type> UnlockedTypes = new();
        foreach (var Tuple in UnlockedCost.GetTuples())
        {
            if (UnlockedTypes.Contains(Tuple.Key))
                continue;

            UnlockedTypes.Add(Tuple.Key);
        }

        if (UnlockedTypes.Count == 0)
            return false;

        int RandomIndex = UnityEngine.Random.Range(0, UnlockedTypes.Count);
        RandomType = UnlockedTypes[RandomIndex];
        return true;
    }

    public bool TryGetRandomUnlockedTile(out HexagonConfig.HexagonType RandomType)
    {
        RandomType = default;
        if (!IsFullyLoaded())
            return false;

        if (!Game.TryGetService(out MeshFactory MeshFactory))
            return false;
        
        HexagonConfig.HexagonType UnlockedTypesMask = 0;
        for (int i = 0; i < BuildingConfig.CategoryAmount; i++)
        {
            for (int j = 0; j < BuildingConfig.MaxIndex; j++)
            {
                int Category = (int)BuildingConfig.Categories[i];
                BuildingConfig.Type Type = (BuildingConfig.Type)(1 << j);
                if (!IsIndexInCategory(Category, j))
                    continue;

                if (IsIndexLockedInCategoryIndex(i, j))
                    continue;

                UnlockedTypesMask |= MeshFactory.CreateDataFromType(Type).BuildableOn;
            }
        }

        List<HexagonConfig.HexagonType> UnlockedTypes = new();
        for (int i = 0; i < HexagonConfig.MaxTypeIndex; i++)
        {
            int HasType = ((int)UnlockedTypesMask >> i) & 0x1;
            if (HasType == 0)
                continue;

            HexagonConfig.HexagonType Type = (HexagonConfig.HexagonType)(HasType << i);
            UnlockedTypes.Add(Type);
        }

        if (UnlockedTypes.Count == 0)
            return false;

        int RandomIndex = UnityEngine.Random.Range(0, UnlockedTypes.Count);
        RandomType = UnlockedTypes[RandomIndex];
        return true;
    }

    private bool IsFullyLoaded()
    {
        // can get called after being init, but not yet loaded!
        // should not matter then, cause calling object should be loaded (overwritten) too
        if (LockedTypesPerCategory.Length != BuildingConfig.CategoryAmount)
            return false;

        return true;
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
            Pos = SaveGameManager.AddInt(Bytes, Pos, (int)LockedTypesPerCategory[i]);
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
        int Pos = 0;
        Pos = SaveGameManager.GetInt(Bytes, Pos, out int Count);
        for (int i = 0; i < Count; i++)
        {
            Pos = SaveGameManager.GetInt(Bytes, Pos, out int Temp);
            LockedTypesPerCategory[i] = (BuildingConfig.Type)Temp;
        }
    }

    public void Reset()
    {
        InitializeCategories();
    }

    protected override void StartServiceInternal() {
        Game.RunAfterServiceInit((SaveGameManager SaveGameManager) =>
        {
            if (!SaveGameManager.HasDataFor(ISaveableService.SaveGameType.Unlockables))
            {
                InitializeCategories();
            }

            _OnInit?.Invoke(this);
        });
    }

    protected override void StopServiceInternal() {}

    public delegate void OnUnlock(BuildingConfig.Type Type);
    public static List<Action<BuildingConfig.Type>> _OnUnlock = new();
}
