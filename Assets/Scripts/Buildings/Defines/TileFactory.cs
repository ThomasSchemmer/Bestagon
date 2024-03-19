using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/**
 * Helper class to generate/find the scriptable objects for building/tile types
 */ 
public class TileFactory : GameService
{
    public SerializedDictionary<BuildingData.Type, Tuple<BuildingData, Mesh>> AvailableBuildings = new();
    public SerializedDictionary<HexagonConfig.HexagonType, Mesh> AvailableTiles = new();
    public SerializedDictionary<HexagonConfig.HexagonDecoration, Mesh> AvailableDecorations = new();
    public Mesh UnknownMesh;

    public void Refresh()
    {
        LoadBuildings();
        LoadTiles();
        LoadDecorations();
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

    public Mesh GetMeshForDecoration(HexagonConfig.HexagonDecoration Decoration) {
        if (!AvailableDecorations.ContainsKey(Decoration))
            return null;

        //todo: make sure its only on suitable stuff

        return AvailableDecorations[Decoration];
    }
}
