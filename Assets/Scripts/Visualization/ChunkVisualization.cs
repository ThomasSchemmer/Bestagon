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

        for (int y = 0; y < HexagonConfig.ChunkSize; y++)
        {
            Profiler.BeginSample("ChunkVis");
            for (int x = 0; x < HexagonConfig.ChunkSize; x++) {
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
        Hexes = new HexagonVisualization[HexagonConfig.ChunkSize, HexagonConfig.ChunkSize];

        for (int y = 0; y < HexagonConfig.ChunkSize; y++) {
            for (int x = 0; x < HexagonConfig.ChunkSize; x++) {
                GameObject HexObj = new GameObject();
                HexObj.transform.parent = this.transform;
                Hexes[x, y] = HexObj.AddComponent<HexagonVisualization>();
            }
        }

        BuildingVisualizations = new();
        UnitVisualizations = new();
        DecorationVisualizations = new();
    }

    public bool TryGetHex(Location Location, out HexagonVisualization Hex)
    {
        Hex = null;
        if (Hexes == null)
            return false;
        if (Location.HexLocation.x >= Hexes.GetLength(0) || Location.HexLocation.y >= Hexes.GetLength(1))
            return false;

        Hex = Hexes[Location.HexLocation.x, Location.HexLocation.y];
        return true;
    }

    private void CreateBuildings() {
        if (!Game.TryGetService(out BuildingService Buildings))
            return;

        if (!Buildings.TryGetEntitiesInChunk(Location, out var FoundBuildings))

        foreach (BuildingEntity BuildingData in FoundBuildings) {
            CreateBuilding(BuildingData);
        }
    }

    public void CreateBuilding(BuildingEntity BuildingData) {
        BuildingVisualization Vis = (BuildingVisualization)BuildingVisualization.CreateFromData(BuildingData);
        Vis.transform.parent = transform;
        BuildingVisualizations.Add(Vis);
    }

    private void CreateDecorations()
    {
        if (!Game.TryGetService(out DecorationService DecorationService))
            return;

        if (!DecorationService.TryGetEntitiesInChunk(Location, out List<DecorationEntity> Decorations))
            return;

        foreach (DecorationEntity Decoration in Decorations)
        {
            DecorationVisualization DecorationVis = (DecorationVisualization)DecorationVisualization.CreateFromData(Decoration);
            DecorationVis.transform.parent = transform;
            DecorationVisualizations.Add(DecorationVis);
        }
    }

    private void CreateUnits()
    {
        if (!Game.TryGetService(out Units UnitService))
            return;

        if (!UnitService.TryGetEntitiesInChunk(Location, out List<TokenizedUnitEntity> Units))
            return;

        foreach (TokenizedUnitEntity Unit in Units)
        {
            UnitVisualization UnitVis = (UnitVisualization)UnitVisualization.CreateFromData(Unit);
            UnitVis.transform.parent = transform;
            UnitVisualizations.Add(UnitVis);
        }
    }

    public void Reset() {
        if (BuildingVisualizations == null || UnitVisualizations == null || DecorationVisualizations == null) 
            return;

        foreach (BuildingVisualization Building in BuildingVisualizations) {
            Destroy(Building.gameObject);
        }
        BuildingVisualizations.Clear();

        foreach (UnitVisualization Unit in UnitVisualizations) {
            Destroy(Unit.gameObject);
        }
        UnitVisualizations.Clear();

        foreach (DecorationVisualization Decoration in DecorationVisualizations)
        {
            Destroy(Decoration.gameObject);
        }
        DecorationVisualizations.Clear();
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
        CreateDecorations();
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

    public List<BuildingVisualization> BuildingVisualizations;
    public List<UnitVisualization> UnitVisualizations;
    public List<DecorationVisualization> DecorationVisualizations;
    public Location Location;
    public Coroutine Generator;

    protected HexagonVisualization[,] Hexes;

    private int FinishedVisualizationCount = 0;
}
