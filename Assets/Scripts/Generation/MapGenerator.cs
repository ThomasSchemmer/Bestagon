using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    private static Location[] DirectionA = new Location[] {
        Location.CreateHex(+0, +1),
        Location.CreateHex(+1, +0),
        Location.CreateHex(+0, -1),
        Location.CreateHex(-1, -1),
        Location.CreateHex(-1, +0),
        Location.CreateHex(-1, +1),
    };

    private static Location[] DirectionB = new Location[] {
        Location.CreateHex(+1, +1),
        Location.CreateHex(+1, +0),
        Location.CreateHex(+1, -1),
        Location.CreateHex(+0, -1),
        Location.CreateHex(-1, +0),
        Location.CreateHex(+0, +1),
    };

    public static Location[] GetDirections(Location Location)
    {
        Location[] EvenDirection = (Location.ChunkLocation.y % 2) == 0 ? DirectionA : DirectionB;
        Location[] OddDirection = (Location.ChunkLocation.y % 2) == 0 ? DirectionB : DirectionA;

        return (Location.HexLocation.y % 2) == 0 ? EvenDirection : OddDirection;
    }

    public void Start() {
        Instance = this;
        MainCam = Camera.main;

        CreateChunks();
        GenerateGrid();
        MalaiseData.SpreadInitially();
    }

    public void Update()
    {
        GenerateGrid();
    }

    public void GenerateGrid()
    {
        Location CenterChunk = GetCameraPosChunkSpace();
        if (CenterChunk.Equals(LastCenterChunk))
            return;

        LastCenterChunk = CenterChunk;

        // what chunks are missing by index vector
        HashSet<Location> NecessaryChunks = GetNecessaryChunkIndices(CenterChunk);
        HashSet<Location> UnNecessaryChunks = GetNecessaryChunkIndices(CenterChunk);
        HashSet<Location> UsedChunks = GetAllVisualizedChunkIndices();
        HashSet<Location> UnusedChunks = GetAllVisualizedChunkIndices();
        NecessaryChunks.ExceptWith(UsedChunks);
        UnusedChunks.ExceptWith(UnNecessaryChunks);

        if (NecessaryChunks.Count != UnusedChunks.Count)
        {
            return;
        }

        // take an unused chunk and update it to its new position
        var Enumerator = NecessaryChunks.GetEnumerator();
        foreach (Location Location in UnusedChunks) {
            Enumerator.MoveNext();
            Location TargetLocation = Enumerator.Current;
            if (!TryGetChunkData(Location, out ChunkData UnusedChunkData))
                return;
            if (!TryGetChunkData(TargetLocation, out ChunkData NecessaryChunkData))
                return;

            ChunkVisualization UnusedVis = UnusedChunkData.Visualization;
            StopCoroutine(UnusedVis.Generator);
            UnusedVis.Reset();

            UnusedVis.Generator = StartCoroutine(UnusedVis.GenerateMeshesAsync(NecessaryChunkData, HexMat, MalaiseMat, Meshes));
        }
        Enumerator.Dispose();
    }

    private void CreateChunks()
    {
        Chunks = new ChunkData[HexagonConfig.mapMaxChunk, HexagonConfig.mapMaxChunk];
        ChunkVis = new ChunkVisualization[HexagonConfig.loadedChunkVisualizations, HexagonConfig.loadedChunkVisualizations];

        HashSet<Location> AllChunks = GetAllChunkIndices();
        foreach (Location Location in AllChunks) {
            ChunkData ChunkData = new ChunkData();
            ChunkData.GenerateData(Location);
            Chunks[Location.ChunkLocation.x, Location.ChunkLocation.y] = ChunkData;
        }

        HashSet<Location> NecessaryChunks = GetNecessaryChunkIndices(Location.CreateChunk(0, 0));
        var NecChunkEnumerator = NecessaryChunks.GetEnumerator();
        for (int x = 0; x < HexagonConfig.loadedChunkVisualizations; x++) {
            for (int y = 0; y < HexagonConfig.loadedChunkVisualizations; y++) {
                NecChunkEnumerator.MoveNext();
                Location TargetChunkIndx = NecChunkEnumerator.Current;
                GameObject ChunkVisObj = new GameObject();
                ChunkVisualization Vis = ChunkVisObj.AddComponent<ChunkVisualization>();
                Vis.transform.parent = this.transform;
                Vis.Data = Chunks[TargetChunkIndx.ChunkLocation.x, TargetChunkIndx.ChunkLocation.y];
                Vis.Initialize();
                Vis.Generator = StartCoroutine(Vis.GenerateMeshesAsync(Vis.Data, HexMat, MalaiseMat, Meshes));
                ChunkVis[x, y] = Vis;
            }
        }
    }

    private HashSet<Location> GetNecessaryChunkIndices(Location CenterChunk)
    {
        HashSet<Location> set = new HashSet<Location>();

        for(int x = 0; x < HexagonConfig.loadedChunkVisualizations; x++)
        {
            for (int y = 0; y < HexagonConfig.loadedChunkVisualizations; y++)
            {
                Location Location = CenterChunk + Location.CreateChunk(x, y);
                if (!HexagonConfig.IsValidChunkIndex(Location.ChunkLocation))
                    continue;

                set.Add(Location);
            }
        }

        return set;
    }

    private HashSet<Location> GetAllChunkIndices() {
        HashSet<Location> Set = new HashSet<Location>();

        for (int x = HexagonConfig.mapMinChunk; x < HexagonConfig.mapMaxChunk; x++) {
            for (int y = HexagonConfig.mapMinChunk; y < HexagonConfig.mapMaxChunk; y++) {
                Set.Add(Location.CreateChunk(x, y));
            }
        }

        return Set;
    }

    private HashSet<Location> GetAllVisualizedChunkIndices() {
        HashSet<Location> Set = new();

        foreach (ChunkVisualization Vis in ChunkVis) {
            bool bIsUsed = Vis.Data != null;
            if (!bIsUsed)
                continue;

            Set.Add(Vis.Data.Location);
        }
        return Set;
    }

    public static List<HexagonVisualization> GetNeighbours(HexagonVisualization Hex) {
        List<HexagonVisualization> Neighbours = new List<HexagonVisualization>();
        List<Location> NeighbourTileLocations = GetNeighbourTileLocations(Hex.Location);

        foreach (Location NeighbourTile in NeighbourTileLocations) {
            if (!TryGetHexagon(NeighbourTile, out HexagonVisualization Neighbour))
                continue;

            Neighbours.Add(Neighbour);
        }

        return Neighbours;

    }

    public static List<HexagonConfig.HexagonType> GetNeighbourTypes(Location Location) {
        List<HexagonConfig.HexagonType> Types = new List<HexagonConfig.HexagonType>();
        List<HexagonData> NeighbourDatas = GetNeighboursData(Location);

        foreach (HexagonData NeighbourData in NeighbourDatas) {
            Types.Add(NeighbourData.Type);
        }

        return Types;
    }

    public static List<HexagonData> GetNeighboursData(Location Location) {
        List<HexagonData> NeighbourDatas = new List<HexagonData>();
        List<Location> NeighbourTileLocations = GetNeighbourTileLocations(Location);

        foreach (Location NeighbourTile in NeighbourTileLocations) {
            if (!TryGetHexagonData(NeighbourTile, out HexagonData Data))
                continue;

            NeighbourDatas.Add(Data);
        }

        return NeighbourDatas;
    }

    public static HexagonData[] GetNeighboursDataArray(Location Location) {
        HexagonData[] NeighbourDatas = new HexagonData[6];
        List<Location> NeighbourTileLocations = GetNeighbourTileLocations(Location);

        for (int i = 0; i < NeighbourTileLocations.Count; i++){
            if (TryGetHexagonData(NeighbourTileLocations[i], out HexagonData Data)) {
                NeighbourDatas[i] = Data;
            } else {
                NeighbourDatas[i] = null;
            }
        }

        return NeighbourDatas;
    }

    public static List<Location> GetNeighbourTileLocations(Location Location) {
        List<Location> NeighbourLocations = new();
        Location[] Directions = GetDirections(Location);

        foreach (Location Direction in Directions) {
            NeighbourLocations.Add(Location + Direction);
        }
        return NeighbourLocations;
    }

    public static bool TryGetHexagon(Location Location, out HexagonVisualization Hex) {
        Hex = null;

        if (!TryGetChunkData(Location, out ChunkData ChunkData))
            return false;

        // cannot get the actual hex object if its not visualized
        if (!ChunkData.Visualization)
            return false;

        if (!HexagonConfig.IsValidHexIndex(Location.HexLocation)) 
            return false;

        Hex = ChunkData.Visualization.Hexes[Location.HexLocation.x, Location.HexLocation.y];
        return Hex != null;
    }

    public static bool TryGetHexagonData(Location Location, out HexagonData HexData) {
        HexData = null;

        if (!TryGetChunkData(Location, out ChunkData ChunkData))
            return false;

        if (!HexagonConfig.IsValidHexIndex(Location.HexLocation))
            return false;

        HexData = ChunkData.HexDatas[Location.HexLocation.x, Location.HexLocation.y];
        return HexData != null;
    }

    public static bool IsBuildingAt(Location Location) {
        if (!TryGetChunkData(Location, out ChunkData Chunk))
            return false;

        return Chunk.IsBuildingAt(Location);
    }

    public static bool TryGetBuildingAt(Location Location, out BuildingData Data) {
        Data = null;

        if (!TryGetChunkData(Location, out ChunkData Chunk))
            return false;

        return Chunk.TryGetBuildingAt(Location, out Data);
    }

    public static void AddBuilding(BuildingData BuildingData) {
        if (!TryGetHexagonData(BuildingData.Location, out HexagonData HexData))
            return;

        if (!TryGetChunkData(BuildingData.Location, out ChunkData Chunk))
            return;

        Chunk.Buildings.Add(BuildingData);

        // if the chunk is currently being shown, force create the building
        if (Chunk.Visualization == null)
            return;

        Chunk.Visualization.CreateBuilding(BuildingData);
    }

    public static Production GetProductionPerTurn() {
        Production Production = new();
        foreach (ChunkData Data in Instance.Chunks) {
            Production += Data.GetProductionPerTurn();
        }

        return Production;
    }

    private Location GetCameraPosChunkSpace()
    {
        // shoot a ray into the middle of the screen
        Plane Plane = new Plane(Vector3.up, Vector3.zero);
        Ray ViewRay = new Ray(MainCam.transform.position, MainCam.transform.forward);
        if (!Plane.Raycast(ViewRay, out float t))
            return Location.Zero;

        //transform this into the (offset) hit chunk, thats the new center
        Vector3 ScreenCenterWorldSpace = ViewRay.GetPoint(t);
        Location CenterLocation = Location.CreateChunk(1, 1);
        Location ChunkLocation = Location.CreateChunk(HexagonConfig.WorldSpaceToChunkSpace(ScreenCenterWorldSpace));
        return ChunkLocation - CenterLocation;
    }

    public static bool TryGetChunkData(Location Location, out ChunkData Chunk) {
        Chunk = null;
        if (!Instance) 
            return false;

        if (!HexagonConfig.IsValidChunkIndex(Location.ChunkLocation)) 
            return false;

        Chunk = Instance.Chunks[Location.ChunkLocation.x, Location.ChunkLocation.y];
        return true;
    }

    public ChunkData[,] Chunks;
    public ChunkVisualization[,] ChunkVis;

    public Material HexMat;
    public Material MalaiseMat;
    public List<Mesh> Meshes;

    public static MapGenerator Instance;

    private Camera MainCam;
    private Location LastCenterChunk = Location.MinValue;
}
