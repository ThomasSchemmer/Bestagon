
using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using static BuildingService;

// todo: save spatially effient, either chunks or quadtree etc
public class BuildingService : GameService, ISaveableService, IQuestRegister<BuildingEntity>
{
    protected override void StartServiceInternal()
    {
        _OnInit?.Invoke(this);
    }

    protected override void StopServiceInternal() { }

    public bool IsBuildingAt(Location Location)
    {
        foreach (BuildingEntity Building in Buildings)
        {
            if (Building.GetLocation().Equals(Location))
                return true;
        }
        return false;
    }

    public List<BuildingEntity> GetBuildingsInChunk(Vector2Int ChunkLocation)
    {
        List<BuildingEntity> SelectedBuildings = new List<BuildingEntity>();
        foreach (BuildingEntity Building in Buildings)
        {
            if (!Building.GetLocation().ChunkLocation.Equals(ChunkLocation))
                continue;

            SelectedBuildings.Add(Building);
        }
        return SelectedBuildings;
    }

    public bool TryGetBuildingAt(Location Location, out BuildingEntity Data)
    {
        Data = null;

        foreach (BuildingEntity Building in Buildings)
        {
            if (Building.GetLocation().Equals(Location))
            {
                Data = Building;
                return true;
            }
        }

        return false;
    }

    public void DestroyBuildingAt(Location Location)
    {
        if (!TryGetBuildingAt(Location, out BuildingEntity Building))
            return;

        // Workers have been killed previously
        Buildings.Remove(Building);

        string Text = Building.BuildingType.ToString() + " has been destroyed by the malaise";
        MessageSystemScreen.CreateMessage(Message.Type.Warning, Text);

        _OnBuildingDestroyed.ForEach(_ => _.Invoke(Building));
        _OnBuildingsChanged?.Invoke();
        Destroy(Building);
    }

    private int GetBuildingsSize()
    {
        int Size = 0;
        foreach (BuildingEntity Building in Buildings)
        {
            Size += Building.GetSize();
        }
        return Size;
    }

    public int GetSize()
    {
        int BuildingSize = GetBuildingsSize();
        // size info for building and overall size
        return BuildingSize + 2 * sizeof(int);
    }

    public byte[] GetData()
    {
        NativeArray<byte> Bytes = new(GetSize(), Allocator.Temp);

        int Pos = 0;
        // save the size to make reading it easier
        Pos = SaveGameManager.AddInt(Bytes, Pos, GetSize());
        Pos = SaveGameManager.AddInt(Bytes, Pos, Buildings.Count);

        foreach (BuildingEntity Building in Buildings)
        {
            Pos = SaveGameManager.AddSaveable(Bytes, Pos, Building);
        }

        return Bytes.ToArray();
    }

    public void SetData(NativeArray<byte> Bytes)
    {
        // skip overall size info at the beginning
        int Pos = sizeof(int);
        Pos = SaveGameManager.GetInt(Bytes, Pos, out int BuildingsLength);

        Buildings = new();
        for (int i = 0; i < BuildingsLength; i++)
        {
            BuildingEntity Building = ScriptableObject.CreateInstance<BuildingEntity>();
            Pos = SaveGameManager.SetSaveable(Bytes, Pos, Building);
            Buildings.Add(Building);
        }
    }

    public void Reset()
    {
        Buildings = new();
    }

    public void AddBuilding(BuildingEntity Building)
    {
        Buildings.Add(Building);

        _OnBuildingBuilt.ForEach(_ => _.Invoke(Building));
        _OnBuildingsChanged?.Invoke();
    }

    public bool ShouldLoadWithLoadedSize() { return true; }

    public List<BuildingEntity> Buildings = new();


    public delegate void OnBuildingsChanged();
    public static OnBuildingsChanged _OnBuildingsChanged;


    public static ActionList<BuildingEntity> _OnBuildingDestroyed = new();
    public static ActionList<BuildingEntity> _OnBuildingBuilt = new();
}
