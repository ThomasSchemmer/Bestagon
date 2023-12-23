using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/**
 * Helper class to generate/find the scriptable buildings for building types
 */ 
public class BuildingFactory : GameService
{
    public SerializedDictionary<BuildingData.Type, Tuple<BuildingData, Mesh>> AvailableBuildings = new();
    public SerializedDictionary<HexagonConfig.HexagonType, Mesh> AvailableTiles = new();

    public void Refresh()
    {
        LoadBuildings();
        LoadTiles();
    }

    protected override void StartServiceInternal()
    {
        Refresh();
        IsInit = true;
        _OnInit?.Invoke();
    }

    protected override void StopServiceInternal() {}

    private void LoadBuildings()
    {
        AvailableBuildings.Clear();
        string[] guids = AssetDatabase.FindAssets("t:buildingData", new[] { "Assets/Scripts/Buildings" });
        foreach (string guid in guids)
        {
            string Path = AssetDatabase.GUIDToAssetPath(guid);
            string NameWithEnding = Path.Substring(Path.LastIndexOf('/') + 1);
            string Name = NameWithEnding[..NameWithEnding.LastIndexOf(".")];
            BuildingData Building = (BuildingData)AssetDatabase.LoadAssetAtPath(Path, typeof(BuildingData));
            if (!Building)
                continue;

            GameObject MeshObject = Resources.Load("Buildings/"+Name) as GameObject;
            if (!MeshObject || !MeshObject.GetComponent<MeshFilter>()) 
                continue;

            Mesh Mesh = MeshObject.GetComponent<MeshFilter>().sharedMesh;
            if (!Mesh)
                continue;

            Tuple<BuildingData, Mesh> Tuple = new(Building, Mesh);
            AvailableBuildings.Add(Building.BuildingType, Tuple);
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
    }

    public BuildingData CreateFromType(BuildingData.Type Type)
    {
        if (!AvailableBuildings.ContainsKey(Type))
            return null;

        var Entry = AvailableBuildings[Type];
        BuildingData Building = Entry.Key;
        BuildingData Copy = Instantiate(Building);
        return Copy;
    }

    public List<BuildingData.Type> GetUnlockedBuildings()
    {
        List<BuildingData.Type> Result = new();

        foreach (var Tuple in AvailableBuildings)
        {
            Result.Add(Tuple.Key);
        }
        return Result;
    }

    public Mesh GetMeshFromType(BuildingData.Type Type)
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
}
