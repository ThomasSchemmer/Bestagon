using System.Collections.Generic;
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
public class MalaiseData : ISaveableData
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

        Hex.MalaisedState = HexagonData.MalaiseState.Malaised;

        Chunk.DestroyTokensAt(Location);
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
            List<HexagonData> Neighbours = MapGenerator.GetNeighboursData(StartLocations[LocationIndex], false);
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
        if (Hex.MalaisedState != HexagonData.MalaiseState.None)
            return false;

        if (!Game.TryGetService(out MapGenerator MapGenerator))
            return false;

        if (TargetChunk == null && !MapGenerator.TryGetChunkData(Hex.Location, out TargetChunk))
            return false;

        Hex.MalaisedState = HexagonData.MalaiseState.PreMalaise;
        TargetChunk.Malaise.LocationsToMalaise.Add(Hex.Location);
        TargetChunk.Malaise.Infect();

        //now we have to break asap to ensure only one neighbour gets infected
        if (!MapGenerator.TryGetHexagon(Hex.Location, out HexagonVisualization HexVis))
            return true;

        HexVis.VisualizeSelection();
        return true;
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

                if (Hex.MalaisedState != HexagonData.MalaiseState.Malaised)
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

        HexData.MalaisedState = HexagonData.MalaiseState.PreMalaise;
        ChunkData.Malaise.LocationsToMalaise.Add(Location);
        ChunkData.Malaise.Infect();

        if (!MapGenerator.TryGetHexagon(Location, out var HexVis))
            return;

        HexVis.VisualizeSelection();
    }

    public int GetSize()
    {
        // overall size, location count and active flag + ID, spread count and increase
        return sizeof(int) * 5 + sizeof(byte) + LocationsToMalaise.Count * Location.GetStaticSize();
    }

    public byte[] GetData()
    {
        NativeArray<byte> Bytes = new(GetSize(), Allocator.Temp);
        int Pos = 0;
        Pos = SaveGameManager.AddInt(Bytes, Pos, GetSize());
        Pos = SaveGameManager.AddInt(Bytes, Pos, ID);
        Pos = SaveGameManager.AddBool(Bytes, Pos, bIsActive);
        Pos = SaveGameManager.AddInt(Bytes, Pos, LocationsToMalaise.Count);
        for (int i = 0; i <  LocationsToMalaise.Count; i++)
        {
            Pos = SaveGameManager.AddSaveable(Bytes, Pos, LocationsToMalaise[i]);
        }
        Pos = SaveGameManager.AddInt(Bytes, Pos, SpreadCountPerRound);
        Pos = SaveGameManager.AddInt(Bytes, Pos, SpreadCountIncrease);

        return Bytes.ToArray();
    }

    public void SetData(NativeArray<byte> Bytes)
    {
        // the only way this can be called is for temporary chunks created for map writing 
        // do not register these! They will be deleted
        int Pos = sizeof(int);
        Pos = SaveGameManager.GetInt(Bytes, Pos, out ID);
        Pos = SaveGameManager.GetBool(Bytes, Pos, out bIsActive);
        Pos = SaveGameManager.GetInt(Bytes, Pos, out int Count);
        for (int i = 0; i < Count; i++)
        {
            Location Location = new();
            Pos = SaveGameManager.SetSaveable(Bytes, Pos, Location);
            if (!Chunk.TryGetHexAt(Location.HexLocation, out HexagonData TargetHex))
                continue;

            // we can force the current chunk here, cause we are directly writing to it anyway
            MarkToMalaise(TargetHex, Chunk);
        }
        Pos = SaveGameManager.GetInt(Bytes, Pos, out SpreadCountPerRound);
        Pos = SaveGameManager.GetInt(Bytes, Pos, out SpreadCountIncrease);

        // remove pointer
        Chunk = null;
    }

    public bool ShouldLoadWithLoadedSize()
    {
        return true;
    }

    public ChunkData Chunk;
    public bool bIsActive = false;
    public int ID = -1;
    public static int CURRENT_MAX_ID = 0;

    public List<Location> LocationsToMalaise = new();

    public static List<Location> StartLocations = new() {
        new Location(0, 0, 0, 0),
        new Location(0, 0, 0, 1),
    };
    public static bool bHasStarted = false;
    public static int SpreadCountPerRound = 5;
    public static int SpreadCountIncrease = 1;
    public static int GlobalMaxSearchCount = 6;
}
