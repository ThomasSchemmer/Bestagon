using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class HexagonConfig {
    /** How many unity world space units should each hexagon be?*/
    public static Vector3 TileSize = new Vector3(10, 5, 10);

    /** How far should the inner border of a tile be inset? */
    public static float TileBorderWidthMultiplier = 0.9f;

    /** How high should the inner border of a tile be of TileSize.y? */
    public static float TileBorderHeightMultiplier = 0.9f;

    /** How high should the tile be of TileSize.y? */
    public static float TileHeightMultiplier = 0.5f;

    /** How many hexagons should be contained in a chunk in both x and y directions? Needs to be an odd nr */
    public static int chunkSize = 9;

    /** Amount of chunk visualizations in both x and y directions that should be loaded during scrolling in the world, needs to be an odd nr*/
    public static int loadedChunkVisualizations = 3;

    /** Lowest index of a chunk in the world, describes the border location */
    public static int mapMinChunk = 0;

    /** Highest index of a chunk in the world, describes the border location */
    public static int mapMaxChunk = 10;

    /** world space offset in x direction per hex*/
    public static float offsetX = Mathf.Sqrt(3) * TileSize.x;

    /** world space offset in y direction per hex*/
    public static float offsetY = 3.0f / 2.0f * TileSize.z;


    public enum HexagonType : uint {
        DEFAULT,
        Meadow,
        Forest,
        Mountain,
        Ocean,
        Desert,
        Tundra,
        Ice
    }

    // lookup table whether a specific type can have a higher tile 
    public static bool[] CanHaveHeight = {
        false,
        false,
        true,
        true,
        false,
        true,
        true,
        false
    };

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
        int Length = Enum.GetValues(typeof(HexagonType)).Length;
        int Value = Mathf.RoundToInt(UnityEngine.Random.Range(1, Length));// Mathf.PerlinNoise(WorldLocation.x, WorldLocation.z) * Length);
        return (HexagonType)Value;
    }

    public static HexagonType GetTypeAtTileLocation(Vector2Int TileLocation) {
        Vector3 WorldLocation = TileSpaceToWorldSpace(TileLocation);
        HexagonType Type = GetTypeAtWorldLocation(WorldLocation);
        return Type;
    }

    public static float GetHeightAtWorldLocation(Vector3 WorldLocation, HexagonType Type) {
        float Multiplier = 1;
        if (CanHaveHeight[(int)Type]) {
            Multiplier = Mathf.RoundToInt(Mathf.PerlinNoise(WorldLocation.x, WorldLocation.z)) * TileHeightMultiplier + 1;
        }
        return Multiplier * TileSize.y;
    }

    public static float GetHeightAtTileLocation(Vector2Int TileLocation, HexagonType Type) {
        Vector3 WorldLocation = TileSpaceToWorldSpace(TileLocation);
        return GetHeightAtWorldLocation(WorldLocation, Type);
    }

    public static Vector3 GetVertex(int i) {
        float Angle = 60.0f * i * Mathf.Deg2Rad;
        return new Vector3(TileSize.x * Mathf.Sin(Angle), 0, TileSize.z * Mathf.Cos(Angle));
    }

    public static int GetCostsFromTo(Location locationA, Location locationB) {
        if (!MapGenerator.TryGetHexagonData(locationA, out HexagonData DataA))
            return -1;

        if (!MapGenerator.TryGetHexagonData(locationB, out HexagonData DataB))
            return -1;

        if (DataB.bIsMalaised)
            return -1;

        switch (DataB.Type) {
            case HexagonType.Meadow: return 1;
            case HexagonType.Forest: return 2;
            case HexagonType.Ocean: return -1;
            case HexagonType.Mountain: return 3;
            case HexagonType.Desert: return 1;
            default: return -1;
        }
    }
}
