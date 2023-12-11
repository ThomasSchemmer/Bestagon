using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

public class MapGenerator : GameService
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

    protected override void StartServiceInternal() {
        MainCam = Camera.main;
        MinBottomLeft = new Location(HexagonConfig.mapMaxChunk, HexagonConfig.mapMaxChunk, HexagonConfig.chunkSize, HexagonConfig.chunkSize);
        MaxTopRight = new Location(HexagonConfig.mapMinChunk, HexagonConfig.mapMinChunk, 0, 0);

        Game.RunAfterServiceStart((WorldGenerator Generator, BuildingFactory Factory) =>
        {
            CreateChunks();
            GenerateGrid();
            if (Game.Instance.Mode == Game.GameMode.Game)
            {
                MalaiseData.SpreadInitially();
            }

            IsInit = true;
            _OnInit?.Invoke();
        });
    }

    protected override void StopServiceInternal(){}

    public void Update()
    {
        if (!IsInit)
            return;

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

            UnusedVis.Generator = StartCoroutine(UnusedVis.GenerateMeshesAsync(NecessaryChunkData, HexMat, MalaiseMat));
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
                Vis.Generator = StartCoroutine(Vis.GenerateMeshesAsync(Vis.Data, HexMat, MalaiseMat));
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

    public List<HexagonVisualization> GetNeighbours(HexagonVisualization Hex) {
        List<HexagonVisualization> Neighbours = new List<HexagonVisualization>();
        List<Location> NeighbourTileLocations = GetNeighbourTileLocations(Hex.Location);

        foreach (Location NeighbourTile in NeighbourTileLocations) {
            if (!TryGetHexagon(NeighbourTile, out HexagonVisualization Neighbour))
                continue;

            Neighbours.Add(Neighbour);
        }

        return Neighbours;

    }

    public List<HexagonConfig.HexagonType> GetNeighbourTypes(Location Location) {
        List<HexagonConfig.HexagonType> Types = new List<HexagonConfig.HexagonType>();
        List<HexagonData> NeighbourDatas = GetNeighboursData(Location);

        foreach (HexagonData NeighbourData in NeighbourDatas) {
            Types.Add(NeighbourData.Type);
        }

        return Types;
    }

    public List<HexagonData> GetNeighboursData(Location Location, int Range = 1) {
        List<HexagonData> NeighbourDatas = new List<HexagonData>();
        HashSet<Location> NeighbourTileLocations = GetNeighbourTileLocationsInRange(Location, Range);

        foreach (Location NeighbourTile in NeighbourTileLocations) {
            if (!TryGetHexagonData(NeighbourTile, out HexagonData Data))
                continue;

            NeighbourDatas.Add(Data);
        }

        return NeighbourDatas;
    }

    public HexagonData[] GetNeighboursDataArray(Location Location) {
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

    /** Returns the locations of all neighbouring tiles around the target */
    public static List<Location> GetNeighbourTileLocations(Location Location) {
        List<Location> NeighbourLocations = new();
        Location[] Directions = GetDirections(Location);

        foreach (Location Direction in Directions) {
            NeighbourLocations.Add(Location + Direction);
        }
        return NeighbourLocations;
    }

    /**
     * Returns the locations of all neighbouring tiles in a radius around the target. 
     * Since this returns a HashSet its possible to easily check for duplicates
     */
    public static HashSet<Location> GetNeighbourTileLocationsInRange(Location Origin, int Range = 1) {
        HashSet<Location> NeighbourLocations = new();
        HashSet<Location> Origins = new();
        Origins.Add(Origin);

        for (int i = 0; i < Range; i++)
        {
            HashSet<Location> NewAdds = new();
            foreach (Location Location in Origins)
            {
                Location[] Directions = GetDirections(Location);
                foreach (Location Direction in Directions)
                {
                    Location Neighbour = Location + Direction;
                    if (!NeighbourLocations.Contains(Neighbour))
                    {
                        NeighbourLocations.Add(Neighbour);
                        NewAdds.Add(Neighbour);
                    }
                }
            }
            Origins = NewAdds;
        }
        return NeighbourLocations;
    }

    public bool TryGetHexagon(Location Location, out HexagonVisualization Hex) {
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

    public bool TryGetHexagonData(Location Location, out HexagonData HexData) {
        HexData = null;

        if (!TryGetChunkData(Location, out ChunkData ChunkData))
            return false;

        if (!HexagonConfig.IsValidHexIndex(Location.HexLocation))
            return false;

        HexData = ChunkData.HexDatas[Location.HexLocation.x, Location.HexLocation.y];
        return HexData != null;
    }

    public bool TrySetHexagonData(Location Location, HexagonData HexData)
    {
        if (!TryGetChunkData(Location, out ChunkData ChunkData))
            return false;

        if (!HexagonConfig.IsValidHexIndex(Location.HexLocation))
            return false;

        ChunkData.HexDatas[Location.HexLocation.x, Location.HexLocation.y] = HexData;
        int Index = HexagonConfig.GetMapPosFromLocation(Location);
        Vector2 TypeData = HexagonConfig.GetMapDataFromType(HexData.Type);
        HexagonConfig.MapData[Index] = TypeData;
        return true;
    }

    public bool IsBuildingAt(Location Location) {
        if (!TryGetChunkData(Location, out ChunkData Chunk))
            return false;

        return Chunk.IsBuildingAt(Location);
    }

    public bool TryGetBuildingAt(Location Location, out BuildingData Data) {
        Data = null;

        if (!TryGetChunkData(Location, out ChunkData Chunk))
            return false;

        return Chunk.TryGetBuildingAt(Location, out Data);
    }

    public void AddBuilding(BuildingData BuildingData) {
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

    public Production GetProductionPerTurn() {
        Production Production = new();
        foreach (ChunkData Data in Chunks) {
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

    public bool TryGetChunkData(Location Location, out ChunkData Chunk) {
        Chunk = null;

        if (!HexagonConfig.IsValidChunkIndex(Location.ChunkLocation)) 
            return false;

        Chunk = Chunks[Location.ChunkLocation.x, Location.ChunkLocation.y];
        return true;
    }

    public void UpdateMapBounds(ChunkVisualization Vis) {
        Location BottomLeft = new Location(Vis.Data.Location.ChunkLocation, new Vector2Int(0, 0));
        Location TopRight = new Location(Vis.Data.Location.ChunkLocation, new Vector2Int(HexagonConfig.chunkSize, HexagonConfig.chunkSize));
        MinBottomLeft = Location.Min(MinBottomLeft, BottomLeft);
        MaxTopRight = Location.Max(MaxTopRight, TopRight);
    }

    public void GetMapBounds(out Location _MinBottomLeft, out Location _MaxTopRight) {
        _MinBottomLeft = MinBottomLeft;
        _MaxTopRight = MaxTopRight;
    }

    public void GetMapBounds(out Vector3 MinBottomLeftWorld, out Vector3 MaxTopRightWorld) {
        GetMapBounds(out Location BottomLeftMap, out Location TopRightMap);
        MinBottomLeftWorld = BottomLeftMap.WorldLocation;
        MaxTopRightWorld = TopRightMap.WorldLocation;
    }

    public HexagonDTO[] GetDTOs() {
        int Count = HexagonConfig.chunkSize * HexagonConfig.chunkSize * HexagonConfig.mapMaxChunk * HexagonConfig.mapMaxChunk;

        HexagonDTO[] DTOs = new HexagonDTO[Count];

        /* Chunks are world partitions, each with a 2d array of their hexes
         * | 6 7 8 | 6 7 8 |
         * | 3 4 5 | 3 4 5 |
         * | 0 1 2 | 0 1 2 |
         * ------------------
         * | 6 7 8 | 6 7 8 |
         * | 3 4 5 | 3 4 5 |
         * | 0 1 2 | 0 1 2 |
         * 
         * we want one global array for easy texture mapping inside the minimap shader
         * | ...         |
         * | 6 7 8 9 10..|
         * | 0 1 2 3 4 5 |
         * 
         * so we need to query the chunks on line at a time!
         * Better: only update dirty data chunks and insert them directly into old array
         */

        int Index = 0;
        for (int y = 0; y < HexagonConfig.mapMaxChunk; y++) {
            for (int j = 0; j < HexagonConfig.chunkSize; j++) {
                for (int x = 0; x < HexagonConfig.mapMaxChunk; x++) {
                    for (int i = 0; i < HexagonConfig.chunkSize; i++) {
                        DTOs[Index] = Chunks[x, y].HexDatas[i, j].GetDTO();
                        Index++;
                    }
                }
            }
        }

        return DTOs;
    }

    public ChunkData[,] Chunks;
    public ChunkVisualization[,] ChunkVis;

    public Material HexMat;
    public Material MalaiseMat;

    private Camera MainCam;
    private Location LastCenterChunk = Location.MinValue;
    private Location MinBottomLeft, MaxTopRight;
}
