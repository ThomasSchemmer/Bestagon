using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Profiling;

/**
 * Visualization for a specific chunk
 * Information of what to visualize gets passed in via ChunkData and later Buildings
 * Contains a reference to all its hexes, which will be changed/regenerated on runtime
 */
public class ChunkVisualization : MonoBehaviour
{
    public IEnumerator GenerateMeshesAsync(ChunkData Data, Material HexMat) 
    {
        this.name = "Chunk " + Data.Location.ChunkLocation;
        this.Location = Data.Location;
        FinishedVisualizationCount = 0;
        Location MaxDiscoveredLoc = new Location(0, 0, 0, 0);

        for (int y = 0; y < HexagonConfig.chunkSize; y++)
        {
            Profiler.BeginSample("ChunkVis");
            for (int x = 0; x < HexagonConfig.chunkSize; x++) {
                // simply add the base position of the chunk as the bottom left corner
                Location Location = Location.CreateHex(x, y) + Data.Location;
                Hexes[x, y].Init(this, Data, Location, HexMat);
                if (Hexes[x, y].Data.GetDiscoveryState() >= HexagonData.DiscoveryState.Scouted)
                {
                    MaxDiscoveredLoc = Location.Max(MaxDiscoveredLoc, Hexes[x, y].Location);
                }
            }
            Profiler.EndSample();
            yield return null;
        }
        RefreshTokens();

        if (!Game.TryGetService(out MapGenerator MapGenerator))
            yield break;

        MapGenerator.UpdateMapBounds(Data.Location, MaxDiscoveredLoc);
    }

    public void Initialize() {
        Hexes = new HexagonVisualization[HexagonConfig.chunkSize, HexagonConfig.chunkSize];

        for (int y = 0; y < HexagonConfig.chunkSize; y++) {
            for (int x = 0; x < HexagonConfig.chunkSize; x++) {
                GameObject HexObj = new GameObject();
                HexObj.transform.parent = this.transform;
                Hexes[x, y] = HexObj.AddComponent<HexagonVisualization>();
            }
        }

        BuildingVisualizations = new();
        UnitVisualizations = new();
    }

    private void CreateBuildings() {
        if (!Game.TryGetService(out BuildingService Buildings))
            return;

        foreach (BuildingData BuildingData in Buildings.GetBuildingsInChunk(Location.ChunkLocation)) {
            BuildingVisualization Vis = BuildingVisualization.CreateFromData(BuildingData);
            Vis.transform.parent = transform;
            BuildingVisualizations.Add(Vis);
        }
    }

    public void CreateBuilding(BuildingData BuildingData) {
        BuildingVisualization Vis = BuildingVisualization.CreateFromData(BuildingData);
        Vis.transform.parent = transform;
        BuildingVisualizations.Add(Vis);
    }

    private void CreateUnits()
    {
        if (!Game.TryGetService(out Units UnitService))
            return;

        if (!UnitService.TryGetUnitsInChunk(Location, out List<TokenizedUnitData> Units))
            return;

        foreach (TokenizedUnitData Unit in Units)
        {
            UnitVisualization UnitVis = UnitVisualization.CreateFromData(Unit);
            UnitVis.transform.parent = transform;
            UnitVisualizations.Add(UnitVis);
        }
    }

    public void Reset() {
        if (BuildingVisualizations == null || UnitVisualizations == null) 
            return;

        foreach (BuildingVisualization Building in BuildingVisualizations) {
            Destroy(Building.gameObject);
        }
        BuildingVisualizations.Clear();

        foreach (UnitVisualization Unit in UnitVisualizations) {
            Destroy(Unit.gameObject);
        }
        UnitVisualizations.Clear();
    }

    public void OnDestroy()
    {
        Reset();
        foreach (HexagonVisualization Vis in Hexes)
        {
            Destroy(Vis.gameObject);
        }
    }

    public void RefreshTokens() {
        Reset();
        CreateBuildings();
        CreateUnits();
    }

    public void FinishVisualization()
    {
        Interlocked.Increment(ref FinishedVisualizationCount);
        if (FinishedVisualizationCount < Hexes.Length)
            return;

        if (!Game.TryGetService(out MapGenerator MapGenerator))
            return;

        MapGenerator.FinishChunkVisualization();
    }

    public HexagonVisualization[,] Hexes;
    public List<BuildingVisualization> BuildingVisualizations;
    public List<UnitVisualization> UnitVisualizations;
    public Location Location;
    public Coroutine Generator;

    private int FinishedVisualizationCount = 0;
}
