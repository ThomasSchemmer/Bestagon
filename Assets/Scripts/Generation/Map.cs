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

    public HexagonData GetHexagonAtLocation(Location Location)
    {
        int Pos = HexagonConfig.GetMapPosFromLocation(Location);
        return MapData[Pos];
    }

    public float GetWorldHeightAtLocation(Location Location)
    {
        int Pos = HexagonConfig.GetMapPosFromLocation(Location);
        HexagonData Hex = MapData[Pos];
        return HexagonConfig.GetWorldHeightFromTile(Hex);
    }

    public void OverwriteSettings(int TileCount, int ChunkCount)
    {
        MapData = new HexagonData[TileCount];
        HexagonConfig.mapMaxChunk = ChunkCount;
    }

    public void SetDataFromChunk(ChunkData Chunk)
    {
        foreach (HexagonData Hex in Chunk.HexDatas)
        {
            int Pos = HexagonConfig.GetMapPosFromLocation(Hex.Location);
            MapData[Pos] = Hex;
        }
    }

    protected override void StartServiceInternal()
    {
        Game.RunAfterServicesInit((WorldGenerator WorldGenerator, SaveGameManager Manager) =>
        {
            // the savegame will fil the map data on its own, no need to generate new 
            // we still need to query the manager object to ensure its already loaded at that point!
            if (!Manager.HasDataFor(ISaveable.SaveGameType.MapGenerator))
            {
                MapData = Game.Instance.Mode == Game.GameMode.Game ? WorldGenerator.NoiseLand(true) : WorldGenerator.EmptyLand();
            }

            _OnInit?.Invoke();
        });
    }

    protected override void StopServiceInternal() { }
}
