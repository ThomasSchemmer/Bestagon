
using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public class BuildingService : TokenizedEntityProvider<BuildingEntity>, IUnlockableService
{
    public Unlockables<BuildingConfig.Type> UnlockableBuildings = new();

    protected override void StartServiceInternal()
    {
        UnlockableBuildings = new();
        UnlockableBuildings.Init(this);
        _OnInit?.Invoke(this);
    }

    protected override void StopServiceInternal() { }

    public void DestroyBuildingAt(Location Location)
    {
        if (!TryGetEntityAt(Location, out BuildingEntity Building))
            return;

        // Workers have been killed previously
        Entities.Remove(Building);

        string Text = Building.BuildingType.ToString() + " has been destroyed by the malaise";
        MessageSystemScreen.CreateMessage(Message.Type.Warning, Text);

        _OnBuildingDestroyed.ForEach(_ => _.Invoke(Building));
        _OnBuildingsChanged?.Invoke();
        Destroy(Building);
    }

    public override void Reset()
    {
        Entities = new();
    }

    public void AddBuilding(BuildingEntity Building)
    {
        Entities.Add(Building);

        _OnBuildingBuilt.ForEach(_ => _.Invoke(Building));
        _OnBuildingsChanged?.Invoke();
    }

    public bool TryGetRandomUnlockedResource(out Production.Type RandomType)
    {
        RandomType = default;
        if (!IsFullyLoaded())
            return false;

        if (!Game.TryGetService(out MeshFactory MeshFactory))
            return false;

        Production UnlockedCost = new();
        int Seed = UnityEngine.Random.Range(0, 100);
        BuildingConfig.Type UnlockedType = UnlockableBuildings.GetRandomOfState(Seed, Unlockables.State.Unlocked, true, false);
        BuildingEntity Building = MeshFactory.CreateDataFromType(UnlockedType);
        UnlockedCost += Building.Cost;
        Destroy(Building);

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

        int Seed = UnityEngine.Random.Range(0, 100);
        BuildingConfig.Type UnlockedType = UnlockableBuildings.GetRandomOfState(Seed, Unlockables.State.Unlocked, true, false);
        BuildingEntity Building = MeshFactory.CreateDataFromType(UnlockedType);
        HexagonConfig.HexagonType UnlockedHexTypes = Building.BuildableOn;
        Destroy(Building);

        List<HexagonConfig.HexagonType> UnlockedTypes = new();
        for (int i = 0; i < HexagonConfig.MaxTypeIndex; i++)
        {
            int HasType = ((int)UnlockedHexTypes >> i) & 0x1;
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
        return UnlockableBuildings.GetCategoryCount() == 4;
    }

    bool IUnlockableService.IsInit()
    {
        return IsInit;
    }

    public void InitUnlockables()
    {
        AddBuildingCategory(BuildingConfig.CategoryMeadow);
        AddBuildingCategory(BuildingConfig.CategoryDesert);
        AddBuildingCategory(BuildingConfig.CategorySwamp);
        AddBuildingCategory(BuildingConfig.CategoryIce);

        UnlockBuildingCategory(BuildingConfig.UnlockOnStart);
    }

    private void UnlockBuildingCategory(BuildingConfig.Type CategoryType)
    {
        int Mask = (int)CategoryType;
        for (int i = 0; i < BuildingConfig.MaxIndex; i++)
        {
            if ((Mask & (1 << i)) == 0)
                continue;

            UnlockableBuildings[(BuildingConfig.Type)(1 << i)] = Unlockables.State.Unlocked;
        }
    }

    private void AddBuildingCategory(BuildingConfig.Type CategoryType)
    {
        SerializedDictionary<BuildingConfig.Type, Unlockables.State> Category = new();
        int Mask = (int)CategoryType;
        for (int i = 0; i < BuildingConfig.MaxIndex; i++)
        {
            if ((Mask & (1 << i)) == 0)
                continue;

            Category.Add((BuildingConfig.Type)(1 << i), Unlockables.State.Locked);
        }
        UnlockableBuildings.AddCategory(Category);
    }

    public delegate void OnBuildingsChanged();
    public static OnBuildingsChanged _OnBuildingsChanged;

    public static ActionList<BuildingEntity> _OnBuildingDestroyed = new();
    public static ActionList<BuildingEntity> _OnBuildingBuilt = new();
}
