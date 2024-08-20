
using System;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using static HexagonConfig;

/** 
 * Container for the actual map data. 
 * Should not be accessed directly, but queried via MapGenerator to also update the visualization
 */
public class Map : GameService
{
    /** 
     * Contains height and temperature map data 
     * Filled by compute shader with correct size
     */
    public HexagonData[] MapData;

    public SerializedDictionary<Location, HexagonDecoration> AdditionalDecorations = new();

    public int MaxSeedIterations = 20;
    public int MaxRandomSeed = 1000;
    public int MinimumReachableTiles = 5;

    public HexagonData GetHexagonAtLocation(Location Location)
    {
        int Pos = GetMapPosFromLocation(Location);
        return MapData[Pos];
    }

    public float GetWorldHeightAtLocation(Location Location)
    {
        int Pos = GetMapPosFromLocation(Location);
        HexagonData Hex = MapData[Pos];
        return GetWorldHeightFromTile(Hex);
    }

    public void OverwriteSettings(int TileCount, int ChunkCount)
    {
        MapData = new HexagonData[TileCount];
        int TilesPerSide = (int)MathF.Sqrt(TileCount);
        HexagonConfig.ChunkSize = TilesPerSide / ChunkCount;
        HexagonConfig.MapMaxChunk = ChunkCount;
    }

    public void SetDataFromChunk(ChunkData Chunk)
    {
        Chunk.ForEachHex((Hexagon) =>
        {
            int Pos = GetMapPosFromLocation(Hexagon.Location);
            MapData[Pos] = Hexagon;
        });
    }

    private void AddDelayedDecorations()
    {
        if (Game.Instance.Mode != Game.GameMode.Game)
            return;

        foreach (var Tuple in AdditionalDecorations)
        {
            int Pos = GetMapPosFromLocation(Tuple.Key);
            MapData[Pos].Decoration = Tuple.Value;
        }
    }

    protected override void StartServiceInternal()
    {
        // the savegame will fil the map data on its own, no need to generate new 
        // we still need to query the manager object to ensure its already loaded at that point!
        Game.RunAfterServicesInit((WorldGenerator WorldGenerator, SaveGameManager Manager) =>
        {
            if (!Manager.HasDataFor(ISaveableService.SaveGameType.MapGenerator))
            {
                FindValidStartingSeed(WorldGenerator);
            }
            _OnInit?.Invoke(this);
        });
    }

    protected void FindValidStartingSeed(WorldGenerator WorldGenerator)
    {
        if (!Game.TryGetService(out SpawnSystem SpawnSystem))
            return;

        if (Game.Instance.Mode == Game.GameMode.MapEditor)
        {
            MapData = WorldGenerator.EmptyLand();
            return;
        }

        Location StartLocation = Location.CreateFromVector(SpawnSystem.StartLocation);
        bool bHasValidSeed = false;
        int Iteration = 0;

        UnityEngine.Random.InitState(DateTime.Now.Second);
        int Seed = 0;
        int LocationsCount = 0;

        PriorityQueue<int> Seeds = new();

        while (!bHasValidSeed && Iteration < MaxSeedIterations)
        {
            Iteration++;
            Seed = UnityEngine.Random.Range(0, MaxRandomSeed);
            bHasValidSeed = EvaluateSeed(WorldGenerator, SpawnSystem, Seed, Seeds, out LocationsCount);
        }
        if (!bHasValidSeed)
        {
            var BestSeed = Seeds.Dequeue();
            Seed = BestSeed.Value; 
            EvaluateSeed(WorldGenerator, SpawnSystem, Seed, Seeds, out LocationsCount);
        }
        UnityEngine.Debug.Log("Found a seed/start after " + Iteration + " tries - seed: "+Seed+" reachable locations: "+LocationsCount);
        
        AddDelayedDecorations();
    }

    private bool EvaluateSeed(WorldGenerator WorldGenerator, SpawnSystem SpawnSystem, int Seed, PriorityQueue<int> Seeds, out int LocationsCount)
    {
        Location StartLocation = Location.CreateFromVector(SpawnSystem.StartLocation);
        MapData = WorldGenerator.NoiseLand(true, Seed);
        var ReachableLocations = Pathfinding.FindReachableLocationsFrom(StartLocation, SpawnSystem.StartScoutingRange + 2, true, true);
        Seeds.Enqueue(ReachableLocations.Count, Seed);
        LocationsCount = ReachableLocations.Count;
        return LocationsCount >= MinimumReachableTiles;
    }

    protected override void StopServiceInternal() { }
}
