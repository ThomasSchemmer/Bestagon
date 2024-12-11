using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Unity.Collections;
using UnityEngine;

/** 
 * Info containing all malaised data for each chunk. Visualized in @CloudRenderer
 * General idea: Malaise spreads from up to 3 random infected tiles to a random neighbour each. 
 * These will get marked for visual clarity and then become infected the next round
 * Will propagate to other chunks (through turns), increasing infection speed 
 * Note: Doesn't actually store which hexes are malaised, thats in the @HexagonData
 */
public class MalaiseData
{
    public void Init(ChunkData InData) {
        Chunk = InData;
        ID = CURRENT_MAX_ID++;
    }

    public void Spread()
    {
        foreach (var Location in LocationsToMalaise)
        {
            SpreadTo(Location);
        }
        LocationsToMalaise.Clear();
        MarkRandomToMalaise();
    }

    private void SpreadTo(Location Location)
    {
        if (!Game.TryGetService(out MapGenerator MapGenerator))
            return;

        if (!Chunk.TryGetHexAt(Location.HexLocation, out HexagonData Hex))
            return;

        if (IsImmune(Location))
        {
            Hex.RemoveMalaise();
        }
        else
        {
            Hex.SetMalaised();
            Chunk.DestroyTokensAt(Location);
        }

        if (!MapGenerator.TryGetChunkVis(Location, out ChunkVisualization ChunkVis))
            return;

        ChunkVis.RefreshTokens();

        if (!MapGenerator.TryGetHexagon(Location, out HexagonVisualization HexVis))
            return;

        HexVis.VisualizeSelection();
    }

    public void Infect() {
        if (bIsActive)
            return;

        bIsActive = true;

        Register();
    }

    private void Register()
    {
        if (!Game.TryGetService(out CloudRenderer CloudRenderer))
            return;

        CloudRenderer.ActiveMalaises.Add(this.Chunk.Location.ChunkLocation, this);
        CloudRenderer.PassMaterialBuffer();
    }

    public void MarkRandomToMalaise()
    {
        if (!Game.TryGetService(out MapGenerator MapGenerator))
            return;

        List<Location> StartLocations = GetRandomMalaised();

        for (int LocationIndex = 0; LocationIndex < StartLocations.Count; LocationIndex++) 
        {
            List<HexagonData> Neighbours = MapGenerator.GetNeighboursData(StartLocations[LocationIndex].ToSet(), false).ToList();
            int RandomNeighbourIndex = Random.Range(0, Neighbours.Count);
            int MaxSearchCount = Mathf.Min(GlobalMaxSearchCount, Neighbours.Count);

            // take a random neighbour direction and check clockwise for the first uncorrupted tile
            for (int SearchCount = 0; SearchCount < MaxSearchCount; SearchCount++)
            {
                int SelectedNeighbourIndex = (RandomNeighbourIndex + SearchCount) % Neighbours.Count;
                HexagonData Neighbour = Neighbours[SelectedNeighbourIndex];
                if (MarkToMalaise(Neighbour))
                    break;  
            }
        }
    }

    private bool MarkToMalaise(HexagonData Hex, ChunkData TargetChunk = null)
    {
        if (Hex.IsAnyMalaised())
            return false;

        if (!Game.TryGetService(out MapGenerator MapGenerator))
            return false;

        if (TargetChunk == null && !MapGenerator.TryGetChunkData(Hex.Location, out TargetChunk))
            return false;

        if (IsImmune(Hex.Location))
            return false;

        Hex.SetPreMalaised();
        TargetChunk.Malaise.LocationsToMalaise.Add(Hex.Location);
        TargetChunk.Malaise.Infect();

        //now we have to break asap to ensure only one neighbour gets infected
        if (!MapGenerator.TryGetHexagon(Hex.Location, out HexagonVisualization HexVis))
            return true;

        HexVis.VisualizeSelection();
        return true;
    }

    private bool IsImmune(Location Location)
    {
        float MalaiseImmunity = AttributeSet.Get()[AttributeType.MalaiseImmunity].GetAt(Location);
        return MalaiseImmunity > 0;
    }

    private List<Location> GetRandomMalaised()
    {
        List<Location> MalaisedLocations = GetMalaised();
        if (MalaisedLocations.Count < SpreadCountPerRound)
            return MalaisedLocations;

        List<Location> RandomLocations = new(SpreadCountPerRound);
        for (int i = 0; i < SpreadCountPerRound; i++)
        {
            int Index = Random.Range(0, MalaisedLocations.Count);
            RandomLocations.Add(MalaisedLocations[Index]);
        }
        return RandomLocations;
    }

    public List<Location> GetMalaised() {
        List<Location> MalaisedHexes = new();
        for (int y = 0; y < HexagonConfig.ChunkSize; y++)
        {
            for (int x = 0; x < HexagonConfig.ChunkSize; x++)
            {
                if (!Chunk.TryGetHexAt(new(x, y), out HexagonData Hex))
                    continue;

                if (!Hex.IsMalaised())
                    continue;

                MalaisedHexes.Add(Hex.Location);
            }
        }

        return MalaisedHexes;
    }

    public void UnmarkToMalaise(Location Location)
    {
        LocationsToMalaise.Remove(Location);
    }

    public List<HexagonData> GetMalaisedData()
    {
        List<HexagonData> MalaisedHexes = new();
        List<Location> MalaisedLocations = GetMalaised();
        foreach (Location Location in MalaisedLocations)
        {
            if (!Chunk.TryGetHexAt(Location.HexLocation, out HexagonData Hex))
                continue;

            MalaisedHexes.Add(Hex);
        }
        return MalaisedHexes;
    }

    public static void SpreadInitially() {
        if (bHasStarted)
            return;

        bHasStarted = true;

        foreach (Location StartLocation in StartLocations)
        {
            SpreadInitially(StartLocation);
        }
    }

    public static void SpreadInitially(Location Location)
    {
        if (!Game.TryGetService(out MapGenerator MapGenerator))
            return;

        if (!MapGenerator.TryGetChunkData(Location, out ChunkData ChunkData))
            return;

        if (ChunkData.Malaise == null)
            return;

        if (!ChunkData.TryGetHexAt(Location.HexLocation, out HexagonData HexData))
            return;

        HexData.SetPreMalaised();
        ChunkData.Malaise.LocationsToMalaise.Add(Location);
        ChunkData.Malaise.Infect();

        if (!MapGenerator.TryGetHexagon(Location, out var HexVis))
            return;

        HexVis.VisualizeSelection();
    }

    public ChunkData Chunk;
    [SaveableBaseType]
    public bool bIsActive = false;
    [SaveableBaseType]
    public int ID = -1;
    public static int CURRENT_MAX_ID = 0;

    [SaveableList]
    public List<Location> LocationsToMalaise = new();

    public static List<Location> StartLocations = new() {
        new Location(0, 0, 0, 0),
        new Location(0, 0, 0, 1),
    };
    public static bool bHasStarted = false;
    [SaveableBaseType]
    public static int SpreadCountPerRound = 5;
    [SaveableBaseType]
    public static int SpreadCountIncrease = 1;
    public static int GlobalMaxSearchCount = 6;
}
