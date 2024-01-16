using System.Collections;
using System.Collections.Generic;
using UnityEditor.U2D.Aseprite;
using UnityEngine;

/** 
 * Container for the actual map data. 
 * Should not be accessed directly, but queried via MapGenerator to also update the visualization
 */
public class Map : GameService
{
    /** Contains height and temperature map data */
    public HexagonConfig.Tile[] MapData;

    public HexagonConfig.Tile GetTileAtLocation(Location Location)
    {
        int Pos = HexagonConfig.GetMapPosFromLocation(Location);
        return MapData[Pos];
    }

    public float GetWorldHeightAtTileLocation(Location Location)
    {
        int Pos = HexagonConfig.GetMapPosFromLocation(Location);
        HexagonConfig.Tile Tile = MapData[Pos];
        return HexagonConfig.GetWorldHeightFromTile(Tile);
    }

    public void OverwriteSettings(int TileCount, int ChunkCount)
    {
        MapData = new HexagonConfig.Tile[TileCount];
        HexagonConfig.mapMaxChunk = ChunkCount;
    }

    public void SetDataFromChunk(ChunkData Chunk)
    {
        foreach (HexagonData Hex in Chunk.HexDatas)
        {
            int Pos = HexagonConfig.GetMapPosFromLocation(Hex.Location);
            MapData[Pos] = new HexagonConfig.Tile(Hex.Height, Hex.Type);
        }
    }

    protected override void StartServiceInternal()
    {
        Game.RunAfterServiceInit((WorldGenerator WorldGenerator) =>
        {
            MapData = Game.Instance.Mode == Game.GameMode.Game ? WorldGenerator.NoiseLand() : WorldGenerator.EmptyLand();
        });
    }

    protected override void StopServiceInternal() { }
}
