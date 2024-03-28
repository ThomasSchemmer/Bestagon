using System;
using UnityEngine;

public class HexagonConfig {

    /** How many unity world space units should each hexagon be?*/
    public static Vector3 TileSize = new Vector3(10, 5, 10);

    /** How far should the inner border of a tile be inset? */
    public static float TileBorderWidthMultiplier = 0.9f;

    /** How high should the inner border of a tile be of TileSize.y? */
    public static float TileBorderHeightMultiplier = 0.9f;

    /** How many hexagons should be contained in a chunk in both x and y directions? Needs to be an odd nr */
    public static int chunkSize = 8;

    /** Amount of chunk visualizations in both x and y directions that should be loaded during scrolling in the world, needs to be an odd nr*/
    public static int loadedChunkVisualizations = 3;

    /** Lowest index of a chunk in the world, describes the border location */
    public static int mapMinChunk = 0;

    /** Highest index of a chunk in the world, describes the border location */
    public static int mapMaxChunk = 10;

    /** Tile width for the complete map */
    public static int MapWidth
    {
        get { return chunkSize * mapMaxChunk; }
    }

    /** world space offset in x direction per hex*/
    public static float offsetX = Mathf.Sqrt(3) * TileSize.x;

    /** world space offset in y direction per hex*/
    public static float offsetY = 3.0f / 2.0f * TileSize.z;

    public static int MaxTypeIndex = 15;
    public static int MaxDecorationIndex = 1;
    public static int MaxHeightIndex = 4;

    public enum HexagonHeight : uint
    {
        /** Must not have values > 8 to still allow being combined into a byte with Type */
        DeepSea = 0,
        Sea = 1,
        Flat = 2,
        Hill = 3,
        Mountain = 4
    }

    [Flags]
    public enum HexagonType : uint
    {
        /** Must not have values > 32 to still allow being combined into a byte with Height */
        Meadow = 1 << 0,
        Forest = 1 << 1,
        Mountain = 1 << 2,
        Ocean = 1 << 3,
        Desert = 1 << 4,
        Tundra = 1 << 5,
        Ice = 1 << 6,
        RainForest = 1 << 7,
        SparseRainForest = 1 << 8,
        DarkForest = 1 << 9,
        Plains = 1 << 10,
        Savanna = 1 << 11,
        Shrubland = 1 << 12,
        Swamp = 1 << 13,
        Taiga = 1 << 14,
        DeepOcean = 1 << 15,
    }

    public enum HexagonDecoration : uint
    {
        None = 0,
        Ruins = 1
    }

    // lookup table whether a specific type can have a higher tile 
    public static HexagonType CanHaveHeight = HexagonType.Forest | HexagonType.Mountain | HexagonType.Desert | HexagonType.Tundra;

    public static void GlobalTileToChunkAndTileSpace(Vector2Int GlobalPos, out Location Location)
    {
        Vector2Int ChunkPos = TileSpaceToChunkSpace(GlobalPos);
        Vector2Int TilePos = GlobalPos - ChunkSpaceToTileSpace(ChunkPos);
        Location = new Location(ChunkPos, TilePos);
    }

    public static Vector2Int WorldSpaceToChunkSpace(Vector3 WorldPos)
    {
        int y = Mathf.RoundToInt(WorldPos.z / offsetY);
        int x = Mathf.RoundToInt(WorldPos.x / offsetX);
        int ChunkPosX = Mathf.RoundToInt(x / chunkSize);
        int ChunkPosY = Mathf.RoundToInt(y / chunkSize);
        return new Vector2Int(ChunkPosX, ChunkPosY);
    }

    public static Vector3 ChunkSpaceToWorldSpace(Vector2Int ChunkPos)
    {
        Vector2Int TilePos = ChunkSpaceToTileSpace(ChunkPos);
        return TileSpaceToWorldSpace(TilePos);
    }

    public static Vector3 TileSpaceToWorldSpace(Vector2Int TilePos)
    {
        float x = offsetX * TilePos.x + (TilePos.y % 2) * 0.5f * offsetX;
        float y = offsetY * TilePos.y;
        return new Vector3(x, 0, y);
    }
    public static Vector2Int WorldSpaceToTileSpace(Vector3 WorldSpace)
    {
        WorldSpace += new Vector3(0.5f * offsetX, 0, 0.5f * offsetY);
        Vector2Int TilePos = new();
        TilePos.y = (int)((WorldSpace.z)/ offsetY);
        float Offset = (TilePos.y % 2) * 0.5f * offsetX;
        TilePos.x = (int)((WorldSpace.x - Offset) / offsetX);
        return TilePos;
    }

    public static Vector2Int TileSpaceToChunkSpace(Vector2Int TilePos)
    {
        int x = Mathf.FloorToInt(TilePos.x / chunkSize);
        int y = Mathf.FloorToInt(TilePos.y / chunkSize);
        return new Vector2Int(x, y);
    }

    /** returns the bottom left tile of a chunk, aka (0, 0) */
    public static Vector2Int ChunkSpaceToTileSpace(Vector2Int ChunkPos)
    {
        return new Vector2Int(ChunkPos.x * chunkSize, ChunkPos.y * chunkSize);
    }

    public static bool IsValidChunkIndex(Vector2Int Index)
    {
        return Index.x >= mapMinChunk && Index.x < mapMaxChunk &&
                Index.y >= mapMinChunk && Index.y < mapMaxChunk;
    }

    public static bool IsValidHexIndex(Vector2Int Index)
    {
        return Index.x >= 0 && Index.x < chunkSize &&
                Index.y >= 0 && Index.y < chunkSize;
    }

    public static Location GetMaxLocation()
    {
        return new Location(new Vector2Int(mapMaxChunk - 1, mapMaxChunk - 1), new Vector2Int(chunkSize - 1, chunkSize - 1));
    }


    public static Vector3 GetVertex(int i)
    {
        float Angle = 60.0f * i * Mathf.Deg2Rad;
        return new Vector3(TileSize.x * Mathf.Sin(Angle), 0, TileSize.z * Mathf.Cos(Angle));
    }

    public static Location GetHexLocationFromScreenPos(Vector3 ScreenPos, float PlaneHeight)
    {
        Ray Ray = Camera.main.ScreenPointToRay(ScreenPos);
        Vector3 Intersection = GetPointOnPlane(
            new Vector3(0, PlaneHeight, 0), 
            Vector3.up, 
            Ray.origin, 
            Ray.direction
        );

        Vector2 GlobalTileCoords = WorldSpaceToTileSpace(Intersection);

        int ChunkX = (int)(GlobalTileCoords.x / chunkSize);
        int ChunkY = (int)(GlobalTileCoords.y / chunkSize);
        int HexX = (int)(GlobalTileCoords.x - ChunkX * chunkSize);
        int HexY = (int)(GlobalTileCoords.y - ChunkY * chunkSize);

        Location HexLocation = new Location(ChunkX, ChunkY, HexX, HexY);
        return HexLocation;
    }

    public static Vector3 GetPointOnPlane(Vector3 PlaneOrigin, Vector3 PlaneNormal, Vector3 Origin, Vector3 Direction)
    {
        float A = Vector3.Dot(PlaneOrigin - Origin, PlaneNormal);
        float B = Vector3.Dot(Direction, PlaneNormal);
        float d = A / B;
        return Origin + Direction * d;
    }

    public static int GetCostsFromTo(Location locationA, Location locationB)
    {
        if (!Game.TryGetService(out MapGenerator MapGenerator))
            return -1;

        if (!MapGenerator.TryGetHexagonData(locationA, out HexagonData DataA))
            return -1;

        if (!MapGenerator.TryGetHexagonData(locationB, out HexagonData DataB))
            return -1;

        if (DataB.bIsMalaised)
            return -1;

        int CostsA = GetTraversingCosts(DataA.Type);
        int CostsB = GetTraversingCosts(DataB.Type);
        if (CostsA < 0 || CostsB < 0)
            return -1;

        return Mathf.CeilToInt((CostsA + CostsB) / 2.0f);
    }

    public static int GetTraversingCosts(HexagonType Type)
    {
        switch (Type)
        {
            case HexagonType.Meadow: return 1;
            case HexagonType.Forest: return 1;
            case HexagonType.Mountain: return 3;
            case HexagonType.Ocean: return -1;
            case HexagonType.Desert: return 2;
            case HexagonType.Tundra: return 2;
            case HexagonType.Ice: return 2;
            case HexagonType.RainForest: return 2;
            case HexagonType.SparseRainForest: return 1;
            case HexagonType.DarkForest: return 1;
            case HexagonType.Plains: return 1;
            case HexagonType.Savanna: return 1;
            case HexagonType.Shrubland: return 1;
            case HexagonType.Swamp: return 2;
            case HexagonType.Taiga: return 1;
            case HexagonType.DeepOcean: return -2;
        }
        return -1;
    }


    public static int GetMapPosFromLocation(Location Location)
    {
        Vector2Int GlobalTileLocation = Location.GlobalTileLocation;
        return GlobalTileLocation.y * MapWidth + GlobalTileLocation.x;
    }

    public static float GetWorldHeightFromTile(HexagonData Hexagon)
    {
        float Multiplier = 1;

        if (CanHaveHeight.HasFlag(Hexagon.Type))
        {
            Multiplier = GetWorldHeightFromHeight(Hexagon);
        }
        return Multiplier * TileSize.y;
    }

    private static float GetWorldHeightFromHeight(HexagonData Hexagon)
    {
        switch (Hexagon.HexHeight)
        {
            case HexagonHeight.DeepSea:
            case HexagonHeight.Sea:
            case HexagonHeight.Flat: return 1;
            case HexagonHeight.Hill:
            case HexagonHeight.Mountain: return 1.5f;
        }
        return 0;
    }

    public static int MaskToInt(int Mask, int Max)
    {
        if (Mask == 0)
            return 0;

        for (int i = 0; i < Max; i++)
        {
            if ((Mask & (1 << i)) > 0)
                return i;
        }
        return -1;
    }

    public static int IntToMask(int Number)
    {
        return 1 << (Number);
    }

    public static string GetShortTypeDescription(HexagonType Type)
    {
        string Result = "";
        for (int i = 0; i < 32; i++)
        {
            HexagonType TempType = (HexagonType)(1 << i);
            if (Type.HasFlag(TempType))
            {
                Result = Result + TempType.ToString()[..1] + " ";
            }
        }

        return Result;
    }

}
