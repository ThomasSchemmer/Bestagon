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

        List<Location> ChosenLocations = GetRandomMalaised();

        for (int i = 0; i < ChosenLocations.Count; i++)
        {
            List<HexagonData> Neighbours = MapGenerator.GetNeighboursData(ChosenLocations[i], false);
            int Index = Random.Range(0, Neighbours.Count);
            HexagonData Neighbour = Neighbours[Index];
            if (Neighbour.MalaisedState != HexagonData.MalaiseState.None)
                continue;

            Neighbour.MalaisedState = HexagonData.MalaiseState.PreMalaise;

            if (!MapGenerator.TryGetChunkData(Neighbour.Location, out ChunkData NeighbourChunk))
                continue;

            NeighbourChunk.Malaise.LocationsToMalaise.Add(Neighbour.Location);
            NeighbourChunk.Malaise.Infect();

            if (!MapGenerator.TryGetHexagon(Neighbour.Location, out HexagonVisualization HexVis))
                continue;

            HexVis.VisualizeSelection();
        }
    }

    private List<Location> GetRandomMalaised()
    {
        List<Location> MalaisedLocations = GetMalaised();
        if (MalaisedLocations.Count < 3)
            return MalaisedLocations;

        List<Location> RandomLocations = new(3);
        for (int i = 0; i < 3; i++)
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
}
