using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

/** 
 * Info containing all malaised data for each chunk. For now has its own visualization but that is going to change
 * with shaders later.
 * General idea: Malaise spreads from up to 3 random infected tiles to a random neighbour each. 
 * These will get marked for visual clarity and then become infected the next round
 * Will propagate to other chunks (through turns), increasing infection speed 
 */
public class MalaiseData : ISaveable
{
    public void Init(ChunkData InData) {
        Chunk = InData;
    }

    public void Spread()
    {
        foreach (var Location in LocationsToMalaise)
        {
            SpreadTo(Location);
        }
        MarkRandomToMalaise();
    }

    private void SpreadTo(Location Location)
    {
        if (!Game.TryGetService(out MapGenerator MapGenerator))
            return;

        HexagonData Hex = Chunk.GetHexAt(Location.HexLocation);
        Hex.MalaisedState = HexagonData.MalaiseState.Malaised;

        Chunk.DestroyTokensAt(Location);
        if (Chunk.Visualization != null)
        {
            Chunk.Visualization.RefreshTokens();
        }

        if (!MapGenerator.TryGetHexagon(Location, out HexagonVisualization HexVis))
            return;

        HexVis.VisualizeSelection();
    }

    public void Infect() {
        if (bIsActive)
            return;

        bIsActive = true;

        if (!Game.TryGetService(out Turn Turn))
            return;

        Turn.ActiveMalaises.Add(this);
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
                if (Neighbour.MalaisedState != HexagonData.MalaiseState.None)
                    continue;

                Neighbour.MalaisedState = HexagonData.MalaiseState.PreMalaise;

                //now we have to break asap to ensure only one neighbour gets infected
                if (!MapGenerator.TryGetChunkData(Neighbour.Location, out ChunkData NeighbourChunk))
                    break;

                NeighbourChunk.Malaise.LocationsToMalaise.Add(Neighbour.Location);
                NeighbourChunk.Malaise.Infect();

                if (!MapGenerator.TryGetHexagon(Neighbour.Location, out HexagonVisualization HexVis))
                    break;

                HexVis.VisualizeSelection();
                break;
            }

        }
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
        for (int y = 0; y < HexagonConfig.chunkSize; y++)
        {
            for (int x = 0; x < HexagonConfig.chunkSize; x++)
            {
                if (Chunk.HexDatas[x, y].MalaisedState != HexagonData.MalaiseState.Malaised)
                    continue;

                MalaisedHexes.Add(Chunk.HexDatas[x, y].Location);
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
            MalaisedHexes.Add(Chunk.GetHexAt(Location.HexLocation));
        }
        return MalaisedHexes;
    }

    public static void SpreadInitially() {
        if (bHasStarted)
            return;

        bHasStarted = true;

        HashSet<ChunkData> ChunksToInfect = new();
        foreach (Location StartLocation in StartLocations)
        {
            if (!Game.GetService<MapGenerator>().TryGetChunkData(StartLocation, out ChunkData ChunkData))
                continue;

            if (ChunkData.Malaise == null)
                continue;

            HexagonData HexData = ChunkData.GetHexAt(StartLocation.HexLocation);
            HexData.MalaisedState = HexagonData.MalaiseState.PreMalaise;
            ChunkData.Malaise.LocationsToMalaise.Add(StartLocation);
            ChunksToInfect.Add(ChunkData);
        }

        foreach (ChunkData Chunk in ChunksToInfect)
        {
            Chunk.Malaise.Infect();
        }
    }

    public int GetSize()
    {
        return sizeof(int);
    }

    public byte[] GetData()
    {
        NativeArray<byte> Bytes = new(GetSize(), Allocator.Temp);
        int Pos = 0;
        Pos = SaveGameManager.AddBool(Bytes, Pos, bIsActive);

        return Bytes.ToArray();
    }

    public void SetData(NativeArray<byte> Bytes)
    {
        int Pos = 0;
        Pos = SaveGameManager.GetBool(Bytes, Pos, out bIsActive);
    }

    public ChunkData Chunk;
    public bool bIsActive = false;

    public List<Location> LocationsToMalaise = new();

    public static List<Location> StartLocations = new() {
        new Location(0, 0, 0, 0),
        new Location(0, 0, 0, 1),
    };
    public static bool bHasStarted = false;
    public static int SpreadCountPerRound = 5;
    public static int GlobalMaxSearchCount = 6;
}
