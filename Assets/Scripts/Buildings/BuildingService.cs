
using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

/** 
 * Provides access to any building entity and keeps track of which buildings types the player has unlocked
 */
public class BuildingService : TokenizedEntityProvider<BuildingEntity>, IUnlockableService<BuildingConfig.Type>
{
    public Unlockables<BuildingConfig.Type> UnlockableBuildings = new();

    protected override void StartServiceInternal()
    {
        Game.RunAfterServiceInit((SaveGameManager Manager) =>
        {
            if (Manager.HasDataFor(ISaveableService.SaveGameType.Buildings))
                return;

            UnlockableBuildings = new();
            UnlockableBuildings.Init(this);
            _OnInit?.Invoke(this);
        });
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
        base.Reset();
        UnlockableBuildings = new();
        UnlockableBuildings.Init(this);
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
        if (!IsInit)
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
        if (!IsInit)
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

    bool IUnlockableService<BuildingConfig.Type>.IsInit()
    {
        return IsInit;
    }
    public int GetValueAsInt(BuildingConfig.Type Type)
    {
        return HexagonConfig.MaskToInt((int)Type, 32);
    }

    public BuildingConfig.Type GetValueAsT(int Value)
    {
        return (BuildingConfig.Type)HexagonConfig.IntToMask(Value);
    }

    public void OnLoadUnlockable(BuildingConfig.Type Type, Unlockables.State State)
    {
        // don't need to do anything, as its handled with the card loading
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

    public override int GetSize()
    {
        return base.GetSize() + GetSelfSize();
    }

    private int GetSelfSize()
    {
        return UnlockableBuildings.GetSize();
    }

    public override byte[] GetData()
    {
        // Note: this should really use GetStaticSize(), but static isn't possible for runtime dependent
        // EntityProvider. Since GetSize() would get overriden, the SGM has no way of getting the actual size
        // could be fixed by a workaround with indirect Saveable loading, but its a service, not an easy entity
        // when overriding BuildingService make sure you update this!
        NativeArray<byte> BaseBytes = new(base.GetSize(), Allocator.Temp);
        NativeArray<byte> Bytes = SaveGameManager.GetArrayWithBaseFilled(GetSize(), base.GetSize(), base.GetData(BaseBytes));

        int Pos = base.GetSize();
        Pos = SaveGameManager.AddSaveable(Bytes, Pos, UnlockableBuildings);

        return Bytes.ToArray();
    }

    public override void SetData(NativeArray<byte> Bytes)
    {
        // will be overwritten by the loading
        UnlockableBuildings = new();
        UnlockableBuildings.Init(this);

        base.SetData(Bytes);
        int Pos = base.GetSize(); ;
        Pos = SaveGameManager.SetSaveable(Bytes, Pos, UnlockableBuildings);

        // kill all buildings, but leave the unlocks untouched
        if (Game.IsIn(Game.GameState.CardSelection))
        {
            base.Reset();
        }
    }

    public override void OnLoaded()
    {
        _OnInit?.Invoke(this);
    }

    public delegate void OnBuildingsChanged();
    public static OnBuildingsChanged _OnBuildingsChanged;

    public static ActionList<BuildingEntity> _OnBuildingDestroyed = new();
    public static ActionList<BuildingEntity> _OnBuildingBuilt = new();
}
