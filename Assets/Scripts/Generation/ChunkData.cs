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
                HexagonConfig.Tile Tile = Map.GetTileAtLocation(HexLocation);
                HexagonData Data = new HexagonData();
                Data.Location = HexLocation;
                Data.Type = Tile.Type;
                Data.Height = Tile.Height;

                HexDatas[x, y] = Data;
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
        foreach (BuildingData Building in Buildings) {
            Production BuildingProduction = Building.GetProduction();
            Production += BuildingProduction;
        }

        return Production;
    }

    public override int GetHashCode() {
        return Location.GetHashCode();
    }

    public bool IsBuildingAt(Location Location) {
        foreach (BuildingData Building in Buildings) {
            if (Building.Location.Equals(Location)) 
                return true;
        }
        return false;
    }

    public bool TryGetBuildingAt(Location Location, out BuildingData Data) {
        Data = null;

        foreach (BuildingData Building in Buildings) {
            if (Building.Location.Equals(Location)) {
                Data = Building;
                return true;
            }
        }

        return false;
    }

    public void DestroyBuildingAt(Location Location) {
        if (!TryGetBuildingAt(Location, out BuildingData Building))
            return;

        for (int i = 0; i < Building.Workers.Count; i++) {
            WorkerData Worker = Building.GetWorkerAt(0);
            // not at work? doesn't get deleted here
            if (!Worker.Location.Equals(Building.Location))
                continue;
            
            Building.RemoveWorkerAt(0);
            Workers.ReturnWorker(Worker);
        }
        Buildings.Remove(Building);

        if (!Visualization)
            return;

        Visualization.Refresh();
    }

    public void DestroyAt(Location Location) {
        Workers.TryGetWorkersAt(Location, out List<WorkerData> WorkersOnTile);
        foreach (WorkerData Worker in WorkersOnTile) { 
            Worker.RemoveFromBuilding();
            Workers.RemoveWorker(Worker);
        }

        DestroyBuildingAt(Location);
    }

    private int GetBuildingsSize()
    {
        int Size = 0;
        foreach (BuildingData Building in Buildings)
        {
            Size += Building.GetSize();
        }
        return Size;
    }

    public int GetSize()
    {
        int HexagonSize = HexDatas.Length > 0 ? HexDatas.Length * HexDatas[0, 0].GetSize() : 0;
        int BuildingSize = GetBuildingsSize();
        // Add the length of the lists as well
        return HexagonSize + BuildingSize + 2 * sizeof(int) + Location.GetSize() + Malaise.GetSize();
    }

    public byte[] GetData()
    {
        NativeArray<byte> Bytes = new(GetSize(), Allocator.Temp);
        int Pos = 0;
        Pos = SaveGameManager.AddInt(Bytes, Pos, HexDatas.Length);
        foreach (HexagonData Hexagon in HexDatas)
        {
            Pos = SaveGameManager.AddSaveable(Bytes, Pos, Hexagon);
        }

        Pos = SaveGameManager.AddInt(Bytes, Pos, Buildings.Count);
        foreach (BuildingData Building in Buildings)
        {
            Pos = SaveGameManager.AddSaveable(Bytes, Pos, Building);
        }

        Pos = SaveGameManager.AddSaveable(Bytes, Pos, Location);
        Pos = SaveGameManager.AddSaveable(Bytes, Pos, Malaise);

        return Bytes.ToArray();
    }

    public void SetData(byte[] Dsata)
    {
        throw new System.NotImplementedException();
    }

    public HexagonData[,] HexDatas;
    public List<BuildingData> Buildings = new();
    public Location Location;
    public ChunkVisualization Visualization;
    public MalaiseData Malaise;
}
