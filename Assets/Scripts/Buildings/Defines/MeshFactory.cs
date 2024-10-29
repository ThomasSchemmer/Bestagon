using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static UnitEntity;

/**
 * Helper class to generate/find the scriptable objects for building/tile types
 */ 
public class MeshFactory : GameService
{
    public SerializedDictionary<HexagonConfig.HexagonType, Mesh> AvailableTiles = new();
    public SerializedDictionary<BuildingConfig.Type, Tuple<BuildingEntity, Mesh>> AvailableBuildings = new();
    public SerializedDictionary<DecorationEntity.DType, Tuple<DecorationEntity, GameObject>> AvailableDecorations = new();
    // units can have alternative skins for the same type
    public SerializedDictionary<UType, Tuple<UnitEntity, List<GameObject>>> AvailableUnits = new();
    public Mesh UnknownMesh, UnknownBuildingMesh;
    public GameObject BuildingPrefab, UnitPrefab, DecorationPrefab;


    public void Refresh()
    {
        LoadBuildings();
        LoadTiles();
        LoadDecorations();
        LoadUnits();
    }

    protected override void StartServiceInternal()
    {
        Refresh();
        _OnInit?.Invoke(this);
    }

    protected override void StopServiceInternal() {}

    private void LoadBuildings()
    {
        AvailableBuildings.Clear();
        string[] Types = Enum.GetNames(typeof(BuildingConfig.Type));
        foreach (string Type in Types)
        {
            BuildingEntity BuildingData = Resources.Load("Buildings/Definitions/" + Type) as BuildingEntity;
            if (!BuildingData)
                continue;

            GameObject MeshObject = Resources.Load("Buildings/Prefabs/"+BuildingData.BuildingType) as GameObject;
            Mesh Mesh = (MeshObject != null && MeshObject.GetComponent<MeshFilter>() != null) ? 
                MeshObject.GetComponent<MeshFilter>().sharedMesh :
                UnknownBuildingMesh;
            if (!Mesh)
                continue;

            Tuple<BuildingEntity, Mesh> Tuple = new(BuildingData, Mesh);
            AvailableBuildings.Add(BuildingData.BuildingType, Tuple);
        }
    }

    private void LoadTiles()
    {
        AvailableTiles.Clear();
        var TileTypes = Enum.GetValues(typeof(HexagonConfig.HexagonType));
        foreach (var TileType in TileTypes)
        {
            GameObject MeshObject = Resources.Load("Tiles/" + TileType) as GameObject;
            if (!MeshObject || !MeshObject.GetComponent<MeshFilter>())
                continue;

            Mesh Mesh = MeshObject.GetComponent<MeshFilter>().sharedMesh;
            if (!Mesh)
                continue;

            AvailableTiles.Add((HexagonConfig.HexagonType)TileType, Mesh);
        }

        GameObject UnknownObject = Resources.Load("Tiles/Unknown") as GameObject;
        if (!UnknownObject || !UnknownObject.GetComponent<MeshFilter>())
            return;

        Mesh UnknownMesh = UnknownObject.GetComponent<MeshFilter>().sharedMesh;
        if (!UnknownMesh)
            return;

        this.UnknownMesh = UnknownMesh;
    }

    private void LoadDecorations()
    {
        AvailableDecorations.Clear();
        var DecorationTypes = Enum.GetValues(typeof(DecorationEntity.DType));
        foreach (var Type in DecorationTypes)
        {
            DecorationEntity.DType DecorationType = (DecorationEntity.DType)Type;

            DecorationEntity Decoration = Resources.Load("Decorations/Definitions/" + DecorationType) as DecorationEntity;
            if (!Decoration)
                continue;

            GameObject DecorationObjecet = Resources.Load("Decorations/Prefabs/" + DecorationType) as GameObject;
            Mesh Mesh = null;
            if (!DecorationObjecet || !DecorationObjecet.GetComponent<MeshFilter>())
                continue;

            Mesh = DecorationObjecet.GetComponent<MeshFilter>().sharedMesh;
            if (!Mesh)
                continue;

            AvailableDecorations.Add(DecorationType, new(Decoration, DecorationObjecet));
        }
    }

    private void LoadUnits()
    {
        AvailableUnits.Clear();
        var UnitTypes = Enum.GetValues(typeof(UType));
        foreach (var Type in UnitTypes)
        {
            UType UnitType = (UType)Type;

            UnitEntity UnitData = Resources.Load("Units/Definitions/" + UnitType) as UnitEntity;
            if (!UnitData)
                continue;

            AvailableUnits.Add(UnitType, new(UnitData, UnitData.Prefabs));
        }
    }

    public BuildingEntity CreateDataFromType(BuildingConfig.Type Type)
    {
        if (!AvailableBuildings.ContainsKey(Type))
            return null;

        var Entry = AvailableBuildings[Type];
        BuildingEntity Building = Entry.Key;
        BuildingEntity Copy = Instantiate(Building);
        Copy.Init();
        return Copy;
    }

    public DecorationEntity CreateDataFromType(DecorationEntity.DType Type)
    {
        if (!AvailableDecorations.ContainsKey(Type))
            return null;

        var Entry = AvailableDecorations[Type];
        DecorationEntity Decoration = Entry.Key;
        DecorationEntity Copy = Instantiate(Decoration);
        return Copy;
    }

    public UnitEntity CreateDataFromType(UType Type)
    {
        if (!AvailableUnits.ContainsKey(Type))
            return null;

        var Entry = AvailableUnits[Type];
        UnitEntity Unit = Entry.Key;
        UnitEntity Copy = Instantiate(Unit);
        return Copy;
    }

    public UnitEntity GetDataFromType(UType Type)
    {
        if (!AvailableUnits.ContainsKey(Type))
            return null;

        var Entry = AvailableUnits[Type];
        UnitEntity Unit = Entry.Key;
        return Unit;
    }

    public Mesh GetMeshFromType(UType Type)
    {
        if (!AvailableUnits.ContainsKey(Type))
            return null;

        UnitEntity Entity = AvailableUnits[Type].Key;
        int Index = Entity.GetTargetMeshIndex();
        if (Index < 0)
            return null;

        GameObject UnitObject = AvailableUnits[Type].Value[Index];
        return UnitObject.GetComponent<MeshFilter>().sharedMesh;
    }

    public Mesh GetMeshFromType(DecorationEntity.DType Type)
    {
        if (!AvailableDecorations.ContainsKey(Type))
            return null;

        GameObject DecorationObject = AvailableDecorations[Type].Value;
        return DecorationObject.GetComponent<MeshFilter>().sharedMesh;
    }

    public GameObject GetObjectFromType(UType Type)
    {
        if (!AvailableUnits.ContainsKey(Type))
            return null;

        UnitEntity Entity = AvailableUnits[Type].Key;
        int Index = Entity.GetTargetMeshIndex();
        if (Index < 0)
            return null;

        return AvailableUnits[Type].Value[Index];
    }

    public Mesh GetMeshFromType(BuildingConfig.Type Type)
    {
        if (!AvailableBuildings.ContainsKey(Type))
            return null;

        return AvailableBuildings[Type].Value;
    }

    public Mesh GetMeshFromType(HexagonConfig.HexagonType Type)
    {
        if (!AvailableTiles.ContainsKey(Type))
            return null;

        return AvailableTiles[Type];
    }

    public GameObject GetGameObjectFromType(BuildingConfig.Type Type)
    {
        GameObject Obj = Instantiate(BuildingPrefab);
        Obj.GetComponent<MeshFilter>().sharedMesh = GetMeshFromType(Type);

        return Obj;
    }

    public GameObject GetGameObjectFromType(DecorationEntity.DType Type)
    {
        GameObject Obj = Instantiate(DecorationPrefab);
        Obj.GetComponent<MeshFilter>().sharedMesh = GetMeshFromType(Type);

        return Obj;
    }

    public GameObject GetGameObjectFromType(UType Type)
    {
        GameObject Obj = Instantiate(UnitPrefab);
        Obj.GetComponent<MeshFilter>().sharedMesh = GetMeshFromType(Type);
        
        return Obj;
    }
}
