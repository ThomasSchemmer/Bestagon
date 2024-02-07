using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public class MalaiseData : ISaveable
{
    public void Init(ChunkData InData) {
        Chunk = InData;
    }

    public void Spread() {
        List<HexagonData> Hexes = GetRandomMalaisedHexes();
        foreach (HexagonData Hex in Hexes) {
            Spread(Hex);
        }
    }

    public void Infect() {
        bIsActive = true;

        if (!Game.TryGetService(out Turn Turn))
            return;
        Turn.ActiveMalaises.Add(this);
    }

    private void Spread(HexagonData Data) {
        MapGenerator MapGenerator = Game.GetService<MapGenerator>();
        if (!MapGenerator)
            return;

        List<HexagonData> Neighbours = MapGenerator.GetNeighboursData(Data.Location);
        for (int i = 0; i < 3; i++) {
            int Index = Random.Range(0, Neighbours.Count);
            HexagonData Neighbour = Neighbours[Index];
            Neighbour.bIsMalaised = true;

            if (!MapGenerator.TryGetChunkData(Neighbour.Location, out ChunkData NeighbourChunk))
                continue;

            NeighbourChunk.DestroyAt(Neighbour.Location);

            // update so that we can find it quicker
            if (!NeighbourChunk.Equals(Chunk) && !NeighbourChunk.Malaise.bIsActive) {
                NeighbourChunk.Malaise.Infect();
            }

            NeighbourChunk.Visualization?.RefreshTokens();

            if (!MapGenerator.TryGetHexagon(Neighbour.Location, out HexagonVisualization Hex))
                continue;

            Hex.VisualizeSelection();
        }
    }

    public List<HexagonData> GetRandomMalaisedHexes() {
        List<HexagonData> MalaisedHexes = GetMalaisedHexes();
        if (MalaisedHexes.Count < 3)
            return MalaisedHexes;

        List<HexagonData> RandomHexes = new(3);
        for (int i = 0; i < 3; i++) {
            int Index = Random.Range(0, MalaisedHexes.Count);
            HexagonData RandomHex = MalaisedHexes[Index];
            RandomHexes.Add(RandomHex);
        }
        return RandomHexes;
    }

    public List<HexagonData> GetMalaisedHexes() {
        List<HexagonData> MalaisedHexes = new();
        foreach (HexagonData Data in Chunk.HexDatas) {
            if (Data.bIsMalaised && Data.GetDiscoveryState() >= HexagonData.DiscoveryState.Scouted) {
                MalaisedHexes.Add(Data);
            }
        }

        return MalaisedHexes;
    }

    public static void SpreadInitially() {
        if (bHasStarted)
            return;

        bHasStarted = true;

        foreach (Location StartLocation in StartLocations)
        {
            if (!Game.GetService<MapGenerator>().TryGetChunkData(StartLocation, out ChunkData ChunkData))
                continue;

            if (ChunkData.Malaise == null)
                continue;

            HexagonData HexData = ChunkData.HexDatas[StartLocation.HexLocation.x, StartLocation.HexLocation.y];
            HexData.bIsMalaised = true;
            ChunkData.Malaise.Spread(HexData);
            ChunkData.Malaise.Infect();

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

    public static List<Location> StartLocations = new() {
        new Location(0, 0, 0, 0),
        new Location(0, 0, 0, 1),
        new Location(0, 0, 1, 1),
        new Location(2, 0, 0, 0),
    };
    public static bool bHasStarted = false;
}
