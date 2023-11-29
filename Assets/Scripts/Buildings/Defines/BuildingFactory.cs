using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/**
 * Helper class to generate/find the scriptable buildings for building types
 */ 
public class BuildingFactory : MonoBehaviour
{
    public SerializedDictionary<BuildingData.Type, Tuple<BuildingData, Mesh>> AvailableBuildings = new();
    public SerializedDictionary<HexagonConfig.HexagonType, Mesh> AvailableTiles = new();
    public static BuildingFactory instance;

    public void Start()
    {
        instance = this;
        Refresh();
    }

    public void Refresh()
    {
        LoadBuildings();
        LoadTiles();
    }

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

    public static BuildingData CreateFromType(BuildingData.Type Type)
    {
        if (!instance)
            return null;

        if (!instance.AvailableBuildings.ContainsKey(Type))
            return null;

        var Entry = instance.AvailableBuildings[Type];
        BuildingData Building = Entry.Key;
        BuildingData Copy = Instantiate(Building);
        return Copy;
    }

    public static List<BuildingData.Type> GetUnlockedBuildings()
    {
        List<BuildingData.Type> Result = new();
        if (!instance)
            return Result;

        foreach (var Tuple in instance.AvailableBuildings)
        {
            Result.Add(Tuple.Key);
        }
        return Result;
    }

    public static Mesh GetMeshFromType(BuildingData.Type Type)
    {
        if (!instance)
            return null;

        if (!instance.AvailableBuildings.ContainsKey(Type))
            return null;

        return instance.AvailableBuildings[Type].Value;
    }

    public static Mesh GetMeshFromType(HexagonConfig.HexagonType Type)
    {
        if (!instance)
            return null;

        if (!instance.AvailableTiles.ContainsKey(Type))
            return null;

        return instance.AvailableTiles[Type];
    }
}
