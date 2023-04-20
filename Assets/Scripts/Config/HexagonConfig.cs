using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class HexagonConfig {
    /** How many unity world space units should each hexagon be?*/
    public static float size = 10f;

    /** How many hexagons should be contained in a chunk in both x and y directions? Needs to be an odd nr */
    public static int chunkSize = 9;

    /** Amount of chunk visualizations in both x and y directions that should be loaded during scrolling in the world, needs to be an odd nr*/
    public static int loadedChunkVisualizations = 3;

    /** Lowest index of a chunk in the world, describes the border location */
    public static int mapMinChunk = 0;

    /** Highest index of a chunk in the world, describes the border location */
    public static int mapMaxChunk = 10;

    /** world space offset in x direction per hex*/
    public static float offsetX = Mathf.Sqrt(3) * HexagonConfig.size;

    /** world space offset in y direction per hex*/
    public static float offsetY = 3.0f / 2.0f * HexagonConfig.size;

    public enum HexagonType {
        DEFAULT,
        Meadow,
        Forest,
        Mountain,
        Ocean
    }

    public static void GlobalTileToChunkAndTileSpace(Vector2Int GlobalPos, out Location Location) {
        Vector2Int ChunkPos = TileSpaceToChunkSpace(GlobalPos);
        Vector2Int TilePos = GlobalPos - ChunkSpaceToTileSpace(ChunkPos);
        Location = new Location(ChunkPos, TilePos);
    }

    public static Vector2Int WorldSpaceToChunkSpace(Vector3 WorldPos) {
        int y = Mathf.RoundToInt(WorldPos.z / offsetY);
        int x = Mathf.RoundToInt(WorldPos.x / offsetX);
        int ChunkPosX = Mathf.RoundToInt(x / chunkSize);
        int ChunkPosY = Mathf.RoundToInt(y / chunkSize);
        return new Vector2Int(ChunkPosX, ChunkPosY);
    }

    public static Vector3 ChunkSpaceToWorldSpace(Vector2Int ChunkPos) {
        Vector2Int TilePos = ChunkSpaceToTileSpace(ChunkPos);
        return TileSpaceToWorldSpace(TilePos);
    }

    public static Vector3 TileSpaceToWorldSpace(Vector2Int TilePos) {
        float x = offsetX * TilePos.x + (TilePos.y % 2) * 0.5f * offsetX;
        float y = offsetY * TilePos.y;
        return new Vector3(x, 0, y);
    }

    public static Vector2Int TileSpaceToChunkSpace(Vector2Int TilePos) {
        int x = Mathf.FloorToInt(TilePos.x / chunkSize);
        int y = Mathf.FloorToInt(TilePos.y / chunkSize);
        return new Vector2Int(x, y);
    }

    /** returns the bottom left tile of a chunk, aka (0, 0) */
    public static Vector2Int ChunkSpaceToTileSpace(Vector2Int ChunkPos) {
        return new Vector2Int(ChunkPos.x * chunkSize, ChunkPos.y * chunkSize);
    }

    public static bool IsValidChunkIndex(Vector2Int Index) {
        return Index.x >= mapMinChunk && Index.x < mapMaxChunk &&
                Index.y >= mapMinChunk && Index.y < mapMaxChunk;
    }

    public static bool IsValidHexIndex(Vector2Int Index) {
        return Index.x >= 0 && Index.x < chunkSize &&
                Index.y >= 0 && Index.y < chunkSize;
    }

    public static HexagonType GetTypeAtWorldLocation(Vector3 WorldLocation) {
        int Value = (int)(Mathf.PerlinNoise(WorldLocation.x, WorldLocation.z) * 4 + 1);
        return (HexagonType)Value;
    }

    public static HexagonType GetTypeAtTileLocation(Vector2Int TileLocation) {
        Vector3 WorldLocation = HexagonConfig.TileSpaceToWorldSpace(TileLocation);
        HexagonType Type = GetTypeAtWorldLocation(WorldLocation);
        return Type;
    }

    public static Vector3 GetVertex(int i) {
        float Angle = 60.0f * i * Mathf.Deg2Rad;
        return new Vector3(size * Mathf.Sin(Angle), 0, size * Mathf.Cos(Angle));
    }
}
