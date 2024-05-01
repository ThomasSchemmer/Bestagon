using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static UnitData;

/**
 * Helper class to generate/find the scriptable objects for building/tile types
 */ 
public class MeshFactory : GameService
{
    public SerializedDictionary<BuildingConfig.Type, Tuple<BuildingData, Mesh>> AvailableBuildings = new();
    public SerializedDictionary<HexagonConfig.HexagonType, Mesh> AvailableTiles = new();
    public SerializedDictionary<HexagonConfig.HexagonDecoration, Mesh> AvailableDecorations = new();
    public SerializedDictionary<UnitType, Tuple<UnitData, Mesh>> AvailableUnits = new();
    public Mesh UnknownMesh;
    public GameObject BuildingPrefab, UnitPrefab;


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
        _OnInit?.Invoke();
    }

    protected override void StopServiceInternal() {}

    private void LoadBuildings()
    {
        AvailableBuildings.Clear();
        string[] Types = Enum.GetNames(typeof(BuildingConfig.Type));
        foreach (string Type in Types)
        {
            BuildingData BuildingData = Resources.Load("Buildings/Definitions/" + Type) as BuildingData;
            if (!BuildingData)
                continue;

            GameObject MeshObject = Resources.Load("Buildings/Prefabs/"+BuildingData.BuildingType) as GameObject;
            if (!MeshObject || !MeshObject.GetComponent<MeshFilter>()) 
                continue;

            Mesh Mesh = MeshObject.GetComponent<MeshFilter>().sharedMesh;
            if (!Mesh)
                continue;

            Tuple<BuildingData, Mesh> Tuple = new(BuildingData, Mesh);
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
        var DecorationTypes = Enum.GetValues(typeof(HexagonConfig.HexagonDecoration));
        foreach (var DecorationType in DecorationTypes)
        {
            if ((HexagonConfig.HexagonDecoration)DecorationType == HexagonConfig.HexagonDecoration.None)
                continue;

            GameObject MeshObject = Resources.Load("Decorations/" + DecorationType) as GameObject;
            if (!MeshObject || !MeshObject.GetComponent<MeshFilter>())
                continue;

            Mesh Mesh = MeshObject.GetComponent<MeshFilter>().sharedMesh;
            if (!Mesh)
                continue;

            AvailableDecorations.Add((HexagonConfig.HexagonDecoration)DecorationType, Mesh);
        }
    }

    private void LoadUnits()
    {
        AvailableUnits.Clear();
        var UnitTypes = Enum.GetValues(typeof(UnitType));
        foreach (var Type in UnitTypes)
        {
            UnitType UnitType = (UnitType)Type;

            UnitData UnitData = Resources.Load("Units/Definitions/" + UnitType) as UnitData;
            if (!UnitData)
                continue;

            GameObject MeshObject = Resources.Load("Units/Prefabs/" + UnitType) as GameObject;
            Mesh Mesh = null;
            if (UnitType != UnitType.Worker)
            {
                if (!MeshObject || !MeshObject.GetComponent<MeshFilter>())
                    continue;

                Mesh = MeshObject.GetComponent<MeshFilter>().sharedMesh;
                if (!Mesh)
                    continue;
            }

            AvailableUnits.Add(UnitType, new(UnitData, Mesh));
        }
    }

    public BuildingData CreateDataFromType(BuildingConfig.Type Type)
    {
        if (!AvailableBuildings.ContainsKey(Type))
            return null;

        var Entry = AvailableBuildings[Type];
        BuildingData Building = Entry.Key;
        BuildingData Copy = Instantiate(Building);
        Copy.Init();
        return Copy;
    }

    public UnitData CreateDataFromType(UnitType Type)
    {
        if (!AvailableUnits.ContainsKey(Type))
            return null;

        var Entry = AvailableUnits[Type];
        UnitData Unit = Entry.Key;
        UnitData Copy = Instantiate(Unit);
        return Copy;
    }

    public GameObject GetUnitFromType(UnitType Type)
    {
        if (!AvailableUnits.ContainsKey(Type))
            return null;

        Mesh UnitMesh = AvailableUnits[Type].Value;
        GameObject UnitGO = Instantiate(UnitPrefab);
        UnitGO.GetComponent<MeshFilter>().mesh = UnitMesh;
        return UnitGO;
    }

    public GameObject GetBuildingFromType(BuildingConfig.Type Type)
    {
        if (!AvailableBuildings.ContainsKey(Type))
            return null;

        Mesh BuildingMesh = AvailableBuildings[Type].Value;
        GameObject BuildingGO = Instantiate(BuildingPrefab);
        BuildingGO.GetComponent<MeshFilter>().mesh = BuildingMesh;
        return BuildingGO;
    }

    public Mesh GetMeshFromType(HexagonConfig.HexagonType Type)
    {
        if (!AvailableTiles.ContainsKey(Type))
            return null;

        return AvailableTiles[Type];
    }

    public Mesh GetMeshForDecoration(HexagonConfig.HexagonDecoration Decoration) {
        if (!AvailableDecorations.ContainsKey(Decoration))
            return null;

        //todo: make sure its only on suitable stuff

        return AvailableDecorations[Decoration];
    }
}
