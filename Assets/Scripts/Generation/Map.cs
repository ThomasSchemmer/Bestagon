using System.Collections;
using System.Collections.Generic;
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

    public HexagonConfig.Tile[] BinaryToMap(byte[] Data)
    {
        HexagonConfig.Tile[] Tiles = new HexagonConfig.Tile[Data.Length - 1];
        HexagonConfig.SetChunkCount((int)Data[0]);
        for (int i = 1; i < Data.Length; i++)
        {
            Tiles[i - 1] = new HexagonConfig.Tile(Data[i]);
        }
        return Tiles;
    }

    public void SetData(byte[] Data)
    {
        MapData = BinaryToMap(Data);
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
