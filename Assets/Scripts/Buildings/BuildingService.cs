
using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using static BuildingService;

/** 
 * Provides access to any building entity and keeps track of which buildings types the player has unlocked
 */
public class BuildingService : TokenizedEntityProvider<BuildingEntity>, IUnlockableService<BuildingConfig.Type>
{
    [SaveableClass]
    public Unlockables<BuildingConfig.Type> UnlockableBuildings = new();

    protected override void StartServiceInternal()
    {
        Game.RunAfterServiceInit((SaveGameManager Manager) =>
        {
            if (Manager.HasDataFor(SaveableService.SaveGameType.Buildings))
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

        MalaiseRandomTiles();

        _OnBuildingDestroyed.ForEach(_ => _.Invoke(Building));
        _OnBuildingsChanged?.Invoke();
        Destroy(Building);
    }

    private void MalaiseRandomTiles()
    {
        if (!Game.TryGetService(out MapGenerator MapGenerator))
            return;

        int ToSpawn = (int)AttributeSet.Get()[AttributeType.AmberMalaiseSpawn].CurrentValue;
        for (int i = 0; i < ToSpawn; i++)
        {
            int x = UnityEngine.Random.Range(0, HexagonConfig.MapMaxChunk);
            int y = UnityEngine.Random.Range(0, HexagonConfig.MapMaxChunk);
            int z = UnityEngine.Random.Range(0, HexagonConfig.ChunkSize);
            int w = UnityEngine.Random.Range(0, HexagonConfig.ChunkSize);
            Location Loc = new(x, y, z, w);
            if (!MapGenerator.TryGetChunkData(Loc, out var Chunk))
                return;

            Chunk.Malaise.SpreadTo(Loc);
        }
    }

    protected override void ResetInternal()
    {
        
        UnlockableBuildings = new();
        UnlockableBuildings.Init(this);
    }

    public void AddBuilding(BuildingEntity Building, LocationSet Location)
    {
        Entities.Add(Building);
        Building.BuildAt(Location, LocationSet.GetAngle());

        _OnBuildingBuilt.ForEach(_ => _.Invoke(Building));
        _OnBuildingsChanged?.Invoke();
    }

    public override bool TryCreateNewEntity(int EntityCode, LocationSet Location)
    {
        if (!Game.TryGetService(out MeshFactory Factory))
            return false;

        var Building = Factory.CreateDataFromType((BuildingConfig.Type)EntityCode);
        if (Building == null)
            return false;

        AddBuilding(Building, Location);
        return true;
    }

    public bool TryGetRandomResource(int Seed, Unlockables.State TargetState, bool bCanBeHigher, out Production.Type RandomType)
    {
        RandomType = default;
        if (!IsInit)
            return false;

        if (!Game.TryGetService(out MeshFactory MeshFactory))
            return false;

        Production UnlockedCost = new();
        BuildingConfig.Type UnlockedType = UnlockableBuildings.GetRandomOfState(Seed, TargetState, bCanBeHigher, false);
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
        return (int)Type;
    }

    public BuildingConfig.Type GetValueAsT(int Value)
    {
        return (BuildingConfig.Type)Value;
    }

    public void OnLoadedUnlockable(BuildingConfig.Type Type, Unlockables.State State)
    {
        // don't need to do anything, as its handled with the card loading
    }

    public void OnLoadedUnlockables()
    {
        Game.RunAfterServiceInit((BuildingService Service) =>
        {
            for (int i = 0; i < Service.UnlockableBuildings.GetCategoryCount(); i++)
            {
                var Category = Service.UnlockableBuildings.GetCategory(i);
                foreach (var Tuple in Category)
                {
                    Service.OnLoadedUnlockable(Tuple.Key, Category[Tuple.Key]);
                }
            }
        });
    }

    public void InitUnlockables()
    {
        UnlockableBuildings.AddCategory(BuildingConfig.CategoryMeadowA, BuildingConfig.MaxIndex);
        UnlockableBuildings.AddCategory(BuildingConfig.CategoryMeadowB, BuildingConfig.MaxIndex);
        UnlockableBuildings.AddCategory(BuildingConfig.CategoryDesertA, BuildingConfig.MaxIndex);
        UnlockableBuildings.AddCategory(BuildingConfig.CategoryDesertB, BuildingConfig.MaxIndex);
        UnlockableBuildings.AddCategory(BuildingConfig.CategorySwampA, BuildingConfig.MaxIndex);
        UnlockableBuildings.AddCategory(BuildingConfig.CategorySwampB, BuildingConfig.MaxIndex);
        UnlockableBuildings.AddCategory(BuildingConfig.CategoryIceA, BuildingConfig.MaxIndex);
        UnlockableBuildings.AddCategory(BuildingConfig.CategoryIceB, BuildingConfig.MaxIndex);

        UnlockableBuildings.UnlockCategory(BuildingConfig.UnlockOnStart, BuildingConfig.MaxIndex);
    }

    public override void OnBeforeLoaded()
    {
        // will be overwritten by the loading
        UnlockableBuildings = new();
        UnlockableBuildings.Init(this);
    }

    public override void OnAfterLoaded()
    {
        base.OnAfterLoaded();

        // since we need keep the unlocked buildings, we have to
        // reset the build buildings here manually
        if (Game.IsIn(Game.GameState.CardSelection))
        {
            KillAllEntities();
        }

        _OnInit?.Invoke(this);
        _OnBuildingsChanged?.Invoke();
    }

    public BuildingConfig.Type Combine(BuildingConfig.Type A, BuildingConfig.Type B)
    {
        return A |= B;
    }

    public override int GetAmountOfType(int EntityCode)
    {
        int Count = 0;
        foreach (var Entity in Entities)
        {
            if ((int)Entity.BuildingType != EntityCode)
                continue;

            Count++;
        }
        return Count;
    }

    public delegate void OnBuildingsChanged();
    public static OnBuildingsChanged _OnBuildingsChanged;

    public static ActionList<BuildingEntity> _OnBuildingDestroyed = new();
    public static ActionList<BuildingEntity> _OnBuildingBuilt = new();
}
