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
public class ChunkData
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

    public int GetHexCount()
    {
        return HexDatas.Length;
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

        if (!Buildings.TryGetEntitiesInChunk(Location, out var FoundBuildings))
            return Production;

        foreach (BuildingEntity Building in FoundBuildings) {
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

        if (!Buildings.TryGetEntityAt(Location, out BuildingEntity Building))
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

    private void DestroyDecorationsAt(Location Location)
    {
        if (!Game.TryGetService(out DecorationService Decorations))
            return;

        if (!Decorations.TryGetEntityAt(Location, out var Entity))
            return;

        Decorations.KillEntity(Entity);
    }

    public void DestroyTokensAt(Location Location)
    {
        if (!Game.TryGetServices(out BuildingService Buildings, out MapGenerator MapGenerator))
            return;

        DestroyWorkersAt(Location);
        DestroyUnitsAt(Location);
        DestroyDecorationsAt(Location);

        Buildings.DestroyBuildingAt(Location);

        if (!MapGenerator.TryGetChunkVis(Location, out ChunkVisualization ChunkVis))
            return;

        ChunkVis.RefreshTokens();
    }

    [SaveableClass]
    public Location Location;
    [SaveableClass]
    public MalaiseData Malaise;

    [SaveableArray]
    protected HexagonData[,] HexDatas;
}
