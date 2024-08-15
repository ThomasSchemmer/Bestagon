using System;
using System.Collections.Generic;
using System.Threading;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Assertions;
using static HexagonData;

/** 
 * Main service to generate and display @HexagonVisualizations.
 * Creates @Chunk's out of the @Map data that then get displayed according to the camera position. 
 * Also provides general access functions to get @HexagonData by @Location
 */
public class MapGenerator : GameService, ISaveableService, IQuestRegister<DiscoveryState>
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
        return (Location.GlobalTileLocation.y % 2) == 0 ? DirectionA : DirectionB;
    }

    protected override void StartServiceInternal() {
        MainCam = Camera.main;
        MinBottomLeft = new Location(HexagonConfig.mapMinChunk, HexagonConfig.mapMinChunk, 0, 0);
        MaxTopRight = new Location(HexagonConfig.mapMinChunk, HexagonConfig.mapMinChunk, 0, 0);

        Game.RunAfterServicesInit((Map Map, MeshFactory Factory) =>
        {
            Game.RunAfterServiceInit((SaveGameManager Manager) =>
            {
                // already loaded then - OnInit will be invoked through chunk visualization callbacks
                if (Manager.HasDataFor(ISaveableService.SaveGameType.MapGenerator))
                    return;

                GenerateMap();

                if (Game.Instance.Mode == Game.GameMode.Game)
                {
                    MalaiseData.bHasStarted = false;
                    MalaiseData.SpreadInitially();
                }
            });
        });
    }

    protected override void StopServiceInternal(){}

    public void Update()
    {
        if (!IsInit)
            return;

        GenerateGrid();
    }

    public void GenerateMap()
    {
        if (!Game.TryGetService(out Selectors Selector))
            return;

        Selector.ForceDeselect();
        DestroyChunks();
        CreateChunks();
        GenerateGrid();

        if (!Game.TryGetService(out MiniMap Minimap))
            return;

        Minimap.FillBuffer();
    }

    private void GenerateGrid()
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

        Assert.IsTrue(NecessaryChunks.Count <= UnusedChunks.Count);

        // take an unused chunk and update it to its new position
        var Enumerator = NecessaryChunks.GetEnumerator();
        foreach (Location UnusedLocation in UnusedChunks) {
            Enumerator.MoveNext();
            Location NecessaryLocation = Enumerator.Current;
            // none more needed, we still have some unused vis but thats ok
            if (NecessaryLocation == null)
                continue;

            if (!TryGetChunkData(NecessaryLocation, out ChunkData NecessaryChunkData))
                continue;

            if (!TryGetChunkVis(UnusedLocation, out ChunkVisualization OldVis))
                continue;

            StopCoroutine(OldVis.Generator);
            OldVis.Reset();

            OldVis.Generator = StartCoroutine(OldVis.GenerateMeshesAsync(NecessaryChunkData, HexMat));
            ChunkVis.Remove(UnusedLocation.ChunkLocation);
            ChunkVis.Add(NecessaryLocation.ChunkLocation, OldVis);
        }
        Enumerator.Dispose();
    }

    private void DestroyChunks()
    {
        if (ChunkVis == null)
            return;
        
        foreach (ChunkVisualization Vis in ChunkVis.Values)
        {
            Destroy(Vis.gameObject);
        }
        ChunkVis = null;
        Chunks = null;
    }

    private void CreateChunks()
    {
        FinishedVisualizationCount = 0;
        HexagonConfig.LoadedChunkVisualizations = Mathf.Min(HexagonConfig.MapMaxChunk, HexagonConfig.LoadedChunkVisualizations);
        HexagonConfig.LoadedChunkVisualizations = Mathf.Max(HexagonConfig.LoadedChunkVisualizations, 0);
        Chunks = new ChunkData[HexagonConfig.MapMaxChunk, HexagonConfig.MapMaxChunk];
        ChunkVis = new(HexagonConfig.LoadedChunkVisualizations * HexagonConfig.LoadedChunkVisualizations);

        if (!Game.TryGetService(out Map Map))
            return;

        HashSet<Location> AllChunks = GetAllChunkIndices();
        foreach (Location Location in AllChunks) {
            ChunkData ChunkData = new ChunkData();
            ChunkData.GenerateData(Location);
            Chunks[Location.ChunkLocation.x, Location.ChunkLocation.y] = ChunkData;
        }

        Assert.IsTrue(HexagonConfig.LoadedChunkVisualizations <= HexagonConfig.MapMaxChunk);
        for (int x = 0; x < HexagonConfig.LoadedChunkVisualizations; x++) {
            for (int y = 0; y < HexagonConfig.LoadedChunkVisualizations; y++) {
                ChunkData ChunkData = Chunks[x, y]; 

                GameObject ChunkVisObj = new GameObject();
                ChunkVisualization Vis = ChunkVisObj.AddComponent<ChunkVisualization>();
                Vis.transform.parent = Map.transform;
                Vis.Initialize();
                Vis.Generator = StartCoroutine(Vis.GenerateMeshesAsync(ChunkData, HexMat));
                ChunkVis.Add(new(x, y), Vis);
            }
        }
    }

    private HashSet<Location> GetNecessaryChunkIndices(Location CenterChunk)
    {
        HashSet<Location> set = new HashSet<Location>();

        int Bounds = (HexagonConfig.LoadedChunkVisualizations - 1) / 2;
        Assert.AreEqual(Bounds * 2 + 1, HexagonConfig.LoadedChunkVisualizations);

        for(int x = -Bounds; x <= Bounds; x++)
        {
            for (int y = -Bounds; y <= Bounds; y++)
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

        for (int x = HexagonConfig.mapMinChunk; x < HexagonConfig.MapMaxChunk; x++) {
            for (int y = HexagonConfig.mapMinChunk; y < HexagonConfig.MapMaxChunk; y++) {
                Set.Add(Location.CreateChunk(x, y));
            }
        }

        return Set;
    }

    private HashSet<Location> GetAllVisualizedChunkIndices() {
        HashSet<Location> Set = new();

        foreach (var Tuple in ChunkVis) {
            Assert.AreEqual(Tuple.Key, Tuple.Value.Location.ChunkLocation);
            Set.Add(Tuple.Value.Location);
        }
        return Set;
    }

    public List<HexagonVisualization> GetNeighbours(HexagonVisualization Hex, bool bShouldAddOrigin, int Range = 1) {
        List<HexagonVisualization> Neighbours = new List<HexagonVisualization>();
        if (Hex == null)
            return Neighbours;

        HashSet<Location> NeighbourTileLocations = GetNeighbourTileLocationsInRange(Hex.Location, bShouldAddOrigin, Range);

        foreach (Location NeighbourLocation in NeighbourTileLocations) {
            if (!TryGetHexagon(NeighbourLocation, out HexagonVisualization Neighbour))
                continue;

            Neighbours.Add(Neighbour);
        }

        return Neighbours;

    }

    public List<HexagonData> GetNeighboursData(Location Location, bool bShouldAddOrigin, int Range = 1) {
        List<HexagonData> NeighbourDatas = new List<HexagonData>();
        HashSet<Location> NeighbourTileLocations = GetNeighbourTileLocationsInRange(Location, bShouldAddOrigin, Range);

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
    public static HashSet<Location> GetNeighbourTileLocationsInRange(Location Origin, bool bShouldAddOrigin, int Range = 1) {
        HashSet<Location> NeighbourLocations = new();
        HashSet<Location> Origins = new();
        Origins.Add(Origin);
        if (bShouldAddOrigin)
        {
            NeighbourLocations.Add(Origin);
        }

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

        if (!HexagonConfig.IsValidHexIndex(Location.HexLocation))
            return false;
        
        // cannot get the actual hex object if its not visualized
        if (!TryGetChunkVis(Location, out ChunkVisualization ChunkVis))
            return false;

        Hex = ChunkVis.Hexes[Location.HexLocation.x, Location.HexLocation.y];
        return Hex != null;
    }

    public bool TryGetHexagonData(Location Location, out HexagonData HexData, bool bTakeRawData = false) {
        HexData = null;
        if (!HexagonConfig.IsValidHexIndex(Location.HexLocation))
            return false;

        if (bTakeRawData && Game.TryGetService(out Map Map))
        {
            HexData = Map.GetHexagonAtLocation(Location);
            return true;
        }

        if (!TryGetChunkData(Location, out ChunkData ChunkData))
            return false;

        HexData = ChunkData.HexDatas[Location.HexLocation.x, Location.HexLocation.y];
        return HexData != null;
    }

    public bool TrySetHexagonData(Location Location, HexagonConfig.HexagonHeight Height, HexagonConfig.HexagonType Type)
    {
        if (!TryGetChunkData(Location, out ChunkData ChunkData))
            return false;

        if (!HexagonConfig.IsValidHexIndex(Location.HexLocation))
            return false;

        int Index = HexagonConfig.GetMapPosFromLocation(Location);
        if (!Game.TryGetService(out Map Map))
            return false;

        Map.MapData[Index] = new(Height, Type);

        ChunkData.HexDatas[Location.HexLocation.x, Location.HexLocation.y].Type = Type;
        ChunkData.HexDatas[Location.HexLocation.x, Location.HexLocation.y].HexHeight = Height;
        return true;
    }


    public void AddBuilding(BuildingData BuildingData) {
        if (!Game.TryGetService(out BuildingService Buildings))
            return;

        if (!TryGetChunkData(BuildingData.Location, out ChunkData Chunk))
            return;

        Buildings.AddBuilding(BuildingData);

        // if the chunk is currently being shown, force create the building
        if (!TryGetChunkVis(BuildingData.Location, out ChunkVisualization ChunkVis))
            return;

        ChunkVis.CreateBuilding(BuildingData);
    }

    public Production GetProductionPerTurn(bool bIsSimulated) {
        Production Production = new();
        foreach (ChunkData Data in Chunks) {
            Production += Data.GetProductionPerTurn(bIsSimulated);
        }

        return Production;
    }

    private Location GetCameraPosChunkSpace()
    {
        Vector3 HexWorldPos = HexagonConfig.GetHexMappedWorldPosition(MainCam, MainCam.transform.position);
        Vector2Int TileSpace = HexagonConfig.WorldSpaceToTileSpace(HexWorldPos);
        Vector2Int ChunkSpace = HexagonConfig.TileSpaceToChunkSpace(TileSpace);
        return new Location(ChunkSpace, new(0, 0));
    }

    public bool TryGetChunkData(Location Location, out ChunkData Chunk) {
        Chunk = null;

        if (Chunks == null)
            return false;

        if (!HexagonConfig.IsValidChunkIndex(Location.ChunkLocation)) 
            return false;

        Chunk = Chunks[Location.ChunkLocation.x, Location.ChunkLocation.y];
        return true;
    }

    public bool TryGetChunkVis(Location Location, out ChunkVisualization Visualization)
    {
        Visualization = null;
        if (ChunkVis == null)
            return false;

        return ChunkVis.TryGetValue(Location.ChunkLocation, out Visualization);
    }

    public void UpdateMapBounds(Location MinDiscoveredLoc, Location MaxDiscoveredLoc) {
        MinBottomLeft = Location.Min(MinBottomLeft, MinDiscoveredLoc);
        MaxTopRight = Location.Max(MaxTopRight, MaxDiscoveredLoc);
        MinBottomLeft = MinBottomLeft.GetMirrorLocation(true);
        MaxTopRight = MaxTopRight.GetMirrorLocation(false);

        if (!Game.TryGetService(out MiniMap Map))
            return;

        Map.FillBuffer();
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

    public void FinishChunkVisualization()
    {
        Interlocked.Increment(ref FinishedVisualizationCount);
        if (FinishedVisualizationCount >= ChunkVis.Count && !IsInit)
        {
            _OnInit?.Invoke(this);
        }
    }

    public void ForEachChunk(Action<ChunkData> Action)
    {
        foreach (var Chunk in Chunks)
        {
            Action(Chunk);
        }
    }

    public void InvokeDiscovery(DiscoveryState DiscoveryState)
    {
        _OnDiscoveredTile.ForEach(_ => _.Invoke(DiscoveryState));
    }

    public HexagonDTO[] GetDTOs() {
        int Count = HexagonConfig.ChunkSize * HexagonConfig.ChunkSize * HexagonConfig.MapMaxChunk * HexagonConfig.MapMaxChunk;

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
        for (int y = 0; y < HexagonConfig.MapMaxChunk; y++) {
            for (int j = 0; j < HexagonConfig.ChunkSize; j++) {
                for (int x = 0; x < HexagonConfig.MapMaxChunk; x++) {
                    for (int i = 0; i < HexagonConfig.ChunkSize; i++) {
                        DTOs[Index] = Chunks[x, y].HexDatas[i, j].GetDTO();
                        Index++;
                    }
                }
            }
        }

        return DTOs;
    }

    public int GetMalaiseDTOByteCount()
    {
        // since we pack every malaise info into a bit and the shader needs 32bit variables, we just write it into an uint
        int BitCount = HexagonConfig.ChunkSize * HexagonConfig.ChunkSize * HexagonConfig.MapMaxChunk * HexagonConfig.MapMaxChunk;
        //force the round up to the next higher number
        int IntCount = Mathf.RoundToInt(BitCount / 32.0f + 0.5f);
        return IntCount;
    }

    public uint[] GetMalaiseDTOs()
    {
        if (bAreMalaiseDTOsDirty || MalaiseDTOs == null)
        {
            CreateMalaiseDTOs();
            bAreMalaiseDTOsDirty = false;
        }

        return MalaiseDTOs;
    }

    private void CreateMalaiseDTOs()
    {
        // see GetDTOs for explanation, this merges it even more:
        // one bit per hex, indicating malaise
        // requires the chunks to be multiple of 8 big, best exactly 8
        MalaiseDTOs = new uint[GetMalaiseDTOByteCount()];

        int SizeOfInt = 32;
        int TilesPerRow = HexagonConfig.MapWidth;
        for (int IntIndex = 0; IntIndex < MalaiseDTOs.Length; IntIndex++) { 
            for (int ByteIndex = 0; ByteIndex < SizeOfInt; ByteIndex++)
            {
                int GlobalIndex = IntIndex * SizeOfInt + ByteIndex;
                // ranges from 0..8, which bit of the current byte
                int IndexInByte = ByteIndex % 8;
                // ranges from 0..4, which byte of the current int
                int IndexInInt = ByteIndex / 8;

                int RowIndex = GlobalIndex / TilesPerRow;
                int ColumnIndex = GlobalIndex % TilesPerRow;
                Vector2Int TileIndex = new(ColumnIndex, RowIndex);

                HexagonConfig.GlobalTileToChunkAndTileSpace(TileIndex, out Location TileLocation);
                if (!HexagonConfig.IsValidLocation(TileLocation))
                    continue;

                if (!TryGetHexagonData(TileLocation, out HexagonData Data))
                    continue;

                bool bIsMalaised = Data.IsMalaised() && Data.IsScouted();
                byte NewValue = (byte)((bIsMalaised ? 1 : 0) << (7 - IndexInByte));

                // read part of the int into a byte, then check if the bit position should be set
                uint OldInt = MalaiseDTOs[IntIndex];
                byte OldValue = (byte)((OldInt >> ((3 - IndexInInt) * 8)) & 0xFF);

                // now write it back into the buffer
                NewValue = (byte)(OldValue | NewValue);
                uint NewInt = (uint)(NewValue << ((3 - IndexInInt) * 8));
                MalaiseDTOs[IntIndex] = OldInt | NewInt;
            }
        
        }
    }

    public int GetSize()
    {
        // Tile count and chunk count, overall size
        int Size = sizeof(int) * 3;
        foreach (ChunkData Chunk in Chunks)
        {
            Size += Chunk.GetSize();
        }
        return Size;
    }

    public byte[] GetData()
    {
        if (!Game.TryGetService(out Map Map))
            return new byte[0];

        NativeArray<byte> Bytes = new(GetSize(), Allocator.Temp);
        int Pos = 0;

        Pos = SaveGameManager.AddInt(Bytes, Pos, GetSize());
        Pos = SaveGameManager.AddInt(Bytes, Pos, Map.MapData.Length);
        Pos = SaveGameManager.AddInt(Bytes, Pos, Chunks.Length);
        foreach (ChunkData Chunk in Chunks)
        {
            Pos = SaveGameManager.AddSaveable(Bytes, Pos, Chunk);
        }

        return Bytes.ToArray();
    }

    public void SetData(NativeArray<byte> Bytes)
    {
        if (!Game.TryGetService(out Map Map))
            return;

        //load as temporary chunk data, write the hex data into the map and then create new chunks from the map
        int Pos = sizeof(int);
        Pos = SaveGameManager.GetInt(Bytes, Pos, out int TileCount);
        Pos = SaveGameManager.GetInt(Bytes, Pos, out int ChunkCount);
        int ChunksPerSide = (int)Mathf.Sqrt(ChunkCount);

        Map.OverwriteSettings(TileCount, ChunksPerSide);

        for (int i = 0; i < ChunkCount; i++)
        {
            ChunkData Temp = new();
            Pos = SaveGameManager.SetSaveable(Bytes, Pos, Temp);
            Map.SetDataFromChunk(Temp);
        }
    }

    public void Load()
    {
        GenerateMap();
    }

    public void Reset()
    {
        DestroyChunks();
        
        if (!Game.TryGetService(out Selectors Selector))
            return;

        Selector.ForceDeselect();
    }

    public bool ShouldLoadWithLoadedSize() { return true; }


    public ChunkData[,] Chunks;
    public Dictionary<Vector2Int, ChunkVisualization> ChunkVis;

    public Material HexMat;

    private Camera MainCam;
    private Location LastCenterChunk = Location.MinValue;
    private Location MinBottomLeft, MaxTopRight;
    private int FinishedVisualizationCount = 0;

    public static ActionList<DiscoveryState> _OnDiscoveredTile = new();

    // do not save anything below this line
    private uint[] MalaiseDTOs = null;
    public bool bAreMalaiseDTOsDirty = true;

}
