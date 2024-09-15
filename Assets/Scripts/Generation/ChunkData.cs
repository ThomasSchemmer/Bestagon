using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

/**
 * Container for all chunk related *data*, such as hexagon types.
 * Data only, does not contain the actual visualization/gameobjects, those are in ChunkVisualization.
 * Should be a fixed size and should never get overwritten
 */
[System.Serializable]
public class ChunkData : ISaveableData
{
    public void GenerateData(Location Location)
    {
        this.Location = Location;
        Malaise = new MalaiseData();
        Malaise.Init(this);

        if (!Game.TryGetService(out Map Map))
            return;

        HexDatas = new HexagonData[HexagonConfig.ChunkSize, HexagonConfig.ChunkSize];
        for (int y = 0; y < HexagonConfig.ChunkSize; y++)
        {
            for (int x = 0; x < HexagonConfig.ChunkSize; x++)
            {
                Location HexLocation = new Location(Location.ChunkLocation, new Vector2Int(x, y));
                HexDatas[x, y] = Map.GetHexagonAtLocation(HexLocation);
                HexDatas[x, y].Location = HexLocation;

                // needs to be done here as malaise will never be permanently loaded directly from savefile
                if (HexDatas[x, y].IsPreMalaised())
                {
                    Malaise.LocationsToMalaise.Add(HexLocation);
                    Malaise.Infect();
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

    public Production GetProductionPerTurn(bool bIsSimulated) {
        Production Production = new();
        if (!Game.TryGetService(out BuildingService Buildings))
            return Production;

        foreach (BuildingEntity Building in Buildings.GetBuildingsInChunk(Location.ChunkLocation)) {
            if (bIsSimulated)
            {
                Building.SimulateCurrentFood();
            }
            Production BuildingProduction = Building.GetProduction(bIsSimulated);
            Production += BuildingProduction;
        }

        return Production;
    }

    public override int GetHashCode() {
        return Location.GetHashCode();
    }

    public void ForEachHex(Action<HexagonData> Action)
    {
        foreach (var Hex in HexDatas)
        {
            Action(Hex);
        }
    }

    public bool TryGetHexAt(Vector2Int HexLocation, out HexagonData Hex)
    {
        Hex = null;
        if (HexDatas == null)
            return false;
        if (HexLocation.x >= HexDatas.GetLength(0) || HexLocation.y >= HexDatas.GetLength(1))
            return false;

        Hex = HexDatas[HexLocation.x, HexLocation.y];
        return true;
    }

    private void DestroyWorkersAt(Location Location)
    {
        if (!Game.TryGetServices(out Workers WorkerService, out BuildingService Buildings))
            return;

        if (!Buildings.TryGetBuildingAt(Location, out BuildingEntity Building))
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

        if (!UnitService.TryGetEntityAt(Location, out TokenizedUnitEntity Unit))
            return;

        UnitService.KillEntity(Unit);

        string Text = "One unit has been killed by malaise";
        MessageSystemScreen.CreateMessage(Message.Type.Warning, Text);
    }

    public void DestroyTokensAt(Location Location)
    {
        if (!Game.TryGetServices(out BuildingService Buildings, out MapGenerator MapGenerator))
            return;

        DestroyWorkersAt(Location);
        DestroyUnitsAt(Location);

        Buildings.DestroyBuildingAt(Location);

        if (!MapGenerator.TryGetChunkVis(Location, out ChunkVisualization ChunkVis))
            return;

        ChunkVis.RefreshTokens();
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

        foreach (HexagonData Hexagon in HexDatas)
        {
            Pos = SaveGameManager.AddSaveable(Bytes, Pos, Hexagon);
        }

        Pos = SaveGameManager.AddSaveable(Bytes, Pos, Malaise);

        return Bytes.ToArray();
    }

    public void SetData(NativeArray<byte> Bytes)
    {
        // this can only be called for temporary chunks from map loading
        // will be destroyed!
        Location = Location.Zero;
        Malaise = new();
        Malaise.Init(this);

        // skip overall size info at the beginning
        int Pos = sizeof(int);
        Pos = SaveGameManager.GetInt(Bytes, Pos, out int HexLength);
        Pos = SaveGameManager.SetSaveable(Bytes, Pos, Location);

        int SqrtLength = (int)Mathf.Sqrt(HexLength);

        HexDatas = new HexagonData[SqrtLength, SqrtLength];
        for (int x = 0; x < SqrtLength; x++)
        {
            for (int y = 0; y < SqrtLength; y++)
            {
                HexDatas[x, y] = new HexagonData();
                Pos = SaveGameManager.SetSaveable(Bytes, Pos, HexDatas[x, y]);
            }
        }

        Pos = SaveGameManager.SetSaveable(Bytes, Pos, Malaise);
        Malaise = null;
    }

    public bool ShouldLoadWithLoadedSize() {  return true; }

    public Location Location;
    public MalaiseData Malaise;

    protected HexagonData[,] HexDatas;
}
