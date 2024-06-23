using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

/**
 * Container for all chunk related *data*, such as hexagon types.
 * Data only, does not contain the actual visualization/gameobjects, those are in ChunkVisualization.
 * Should be a fixed size and should never get overwritten
 */
[System.Serializable]
public class ChunkData : ISaveable
{

    public void GenerateData(Location Location)
    {
        this.Location = Location;
        Malaise = new MalaiseData();
        Malaise.Chunk = this;

        if (!Game.TryGetService(out Map Map))
            return;

        HexDatas = new HexagonData[HexagonConfig.chunkSize, HexagonConfig.chunkSize];
        for (int y = 0; y < HexagonConfig.chunkSize; y++)
        {
            for (int x = 0; x < HexagonConfig.chunkSize; x++)
            {
                Location HexLocation = new Location(Location.ChunkLocation, new Vector2Int(x, y));
                HexDatas[x, y] = Map.GetHexagonAtLocation(HexLocation);
                HexDatas[x, y].Location = HexLocation;

                // todo: debug remove
                if (x == 3 && y == 3)
                {
                    HexDatas[x, y].Decoration = HexagonConfig.HexagonDecoration.Tribe;
                }
            }
        }
    }

    public override bool Equals(object obj) {
        if (obj is not ChunkData)
            return false;

        ChunkData Other = obj as ChunkData;
        return Location.Equals(Other.Location);
    }

    public Production GetProductionPerTurn() {
        Production Production = new();
        if (!Game.TryGetService(out BuildingService Buildings))
            return Production;

        foreach (BuildingData Building in Buildings.GetBuildingsInChunk(Location.ChunkLocation)) {
            Production BuildingProduction = Building.GetProduction();
            Production += BuildingProduction;
        }

        return Production;
    }

    public override int GetHashCode() {
        return Location.GetHashCode();
    }

    public HexagonData GetHexAt(Vector2Int HexLocation)
    {
        return HexDatas[HexLocation.x, HexLocation.y];
    }

    private void DestroyWorkersAt(Location Location)
    {
        if (!Game.TryGetServices(out Workers WorkerService, out BuildingService Buildings))
            return;

        if (!Buildings.TryGetBuildingAt(Location, out BuildingData Building))
            return;

        int TempWorkerCount = Building.GetAssignedWorkerCount();
        for (int i = 0; i < TempWorkerCount; i++)
        {
            WorkerService.KillWorker(Building.AssignedWorkers[i]);
        }

        if (TempWorkerCount > 0)
        {
            string Text = TempWorkerCount + " worker " + (TempWorkerCount == 1 ? "has " : "have ") + "been killed by malaise";
            MessageSystemScreen.CreateMessage(Message.Type.Warning, Text);
        }
    }

    private void DestroyUnitsAt(Location Location)
    {
        if (!Game.TryGetService(out Units UnitService))
            return;

        if (!UnitService.TryGetUnitAt(Location, out TokenizedUnitData Unit))
            return;

        UnitService.KillUnit(Unit);

        string Text = "One unit has been killed by malaise";
        MessageSystemScreen.CreateMessage(Message.Type.Warning, Text);
    }

    public void DestroyTokensAt(Location Location)
    {
        if (!Game.TryGetService(out BuildingService Buildings))
            return;

        DestroyWorkersAt(Location);
        DestroyUnitsAt(Location);

        Buildings.DestroyBuildingAt(Location);

        if (!Visualization)
            return;

        Visualization.RefreshTokens();
    }

    public int GetSize()
    {
        int HexagonSize = HexDatas.Length > 0 ? HexDatas.Length * HexDatas[0, 0].GetSize() : 0;
        // Location and malaise, data of hexes, as well as size info for hex and overall size
        return HexagonSize + 2 * sizeof(int) + Location.GetStaticSize() + Malaise.GetSize();
    }

    public byte[] GetData()
    {
        NativeArray<byte> Bytes = new(GetSize(), Allocator.Temp);

        int Pos = 0;
        // save the size to make reading it easier
        Pos = SaveGameManager.AddInt(Bytes, Pos, GetSize());
        Pos = SaveGameManager.AddInt(Bytes, Pos, HexDatas.Length);
        Pos = SaveGameManager.AddSaveable(Bytes, Pos, Location);
        Pos = SaveGameManager.AddSaveable(Bytes, Pos, Malaise);

        // save the lists
        foreach (HexagonData Hexagon in HexDatas)
        {
            Pos = SaveGameManager.AddSaveable(Bytes, Pos, Hexagon);
        }

        return Bytes.ToArray();
    }

    public void SetData(NativeArray<byte> Bytes)
    {
        Location = Location.Zero;
        Malaise = new();

        // skip overall size info at the beginning
        int Pos = sizeof(int);
        Pos = SaveGameManager.GetInt(Bytes, Pos, out int HexLength);
        Pos = SaveGameManager.SetSaveable(Bytes, Pos, Location);
        Pos = SaveGameManager.SetSaveable(Bytes, Pos, Malaise);

        int SqrtLength = (int)Mathf.Sqrt(HexLength);

        HexDatas = new HexagonData[SqrtLength, SqrtLength];
        for (int y = 0; y < SqrtLength; y++)
        {
            for (int x = 0; x < SqrtLength; x++)
            {
                HexDatas[x, y] = new HexagonData();
                Pos = SaveGameManager.SetSaveable(Bytes, Pos, HexDatas[x, y]);
            }
        }
    }

    public bool ShouldLoadWithLoadedSize() {  return true; }

    public HexagonData[,] HexDatas;
    public Location Location;
    public ChunkVisualization Visualization;
    public MalaiseData Malaise;
}
