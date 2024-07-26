using System;
using UnityEngine;

public class HexagonConfig {

    /** How many unity world space units should each hexagon be?*/
    public static Vector3 TileSize = new Vector3(10, 5, 10);

    /** How far should the inner border of a tile be inset? */
    public static float TileBorderWidthMultiplier = 0.9f;

    /** How high should the inner border of a tile be of TileSize.y? */
    public static float TileBorderHeightMultiplier = 0.9f;

    /** How many hexagons should be contained in a chunk in both x and y directions? Needs to be an odd nr 
     * WARNING: if updated fix cloud shader first! bitstuffing doesnt work with arbitrary size!
     */
    public static int chunkSize = 2;

    /** Amount of chunk visualizations in both x and y directions that should be loaded during scrolling in the world, needs to be an odd nr*/
    public static int loadedChunkVisualizations = 3;

    /** Lowest index of a chunk in the world, describes the border location */
    public static int mapMinChunk = 0;

    /** Highest index of a chunk in the world, describes the border location */
    public static int mapMaxChunk = 3;

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
        Ruins = 1,
        Tribe = 2
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

    /** Conversion functions from RedBlobGames */
    static Vector2Int RoundToAxial(float x, float y)
    {
        int xgrid = Mathf.RoundToInt(x);
        int ygrid = Mathf.RoundToInt(y);
        x -= xgrid;
        y -= ygrid;
        int bx = x * x >= y * y ? 1 : 0;
        int dx = Mathf.RoundToInt(x + 0.5f * y) * bx;
        int dy = Mathf.RoundToInt(y + 0.5f * x) * (1 - bx);
        return new Vector2Int(xgrid + dx, ygrid + dy);
    }

    static Vector2Int AxialToOffset(Vector2Int hex)
    {
        int col = (int)(hex.x + (hex.y - (hex.y & 1)) / 2.0);
        int row = hex.y;
        return new Vector2Int(col, row);
    }

    public static Vector2Int WorldSpaceToTileSpace(Vector3 WorldSpace)
    {
        float q = (Mathf.Sqrt(3) / 3.0f * WorldSpace.x - 1.0f / 3 * WorldSpace.z) / 10;
        float r = (2.0f / 3 * WorldSpace.z) / 10;
        return AxialToOffset(RoundToAxial(q, r));
    }

    public static Vector2Int TileSpaceToChunkSpace(Vector2Int TilePos)
    {
        int x = Mathf.FloorToInt(TilePos.x / chunkSize);
        int y = Mathf.FloorToInt(TilePos.y / chunkSize);
        return new Vector2Int(x, y);
    }


    public static Vector3 GetHexMappedWorldPosition(Camera Cam, Vector3 WorldPos)
    {
        Vector2 Box = RayBoxDist(WorldPos, Cam.transform.forward);
        Vector3 BoxStart = WorldPos + Cam.transform.forward * Box.x;
        Vector3 BoxEnd = BoxStart + Cam.transform.forward * Box.y;
        Vector3 MappedWorldPos = (BoxStart + BoxEnd) / 2.0f;
        return MappedWorldPos;
    }

    public static Vector3 GetWorldPosFromMousePosition(Camera Cam)
    {
        float WidthToHeight = (float)Screen.width / Screen.height;
        Vector2 MouseUV = new Vector2(Input.mousePosition.x / Screen.width, Input.mousePosition.y / Screen.height);
        MouseUV = (MouseUV - Vector2.one * 0.5f) * 2;

        float Size = Cam.orthographicSize;
        Vector3 Offset =
            MouseUV.x * Cam.transform.right * Size * WidthToHeight +
            MouseUV.y * Cam.transform.up * Size +
            10 * -Cam.transform.forward;
        Vector3 WorldPos = Cam.transform.position + Offset;
        return WorldPos;
    }

    static Vector3 GetCloudsMin()
    {
        return new(
            -TileSize.x,
            TileSize.y + 0.5f,
            -TileSize.z
        );
    }

    static Vector3 GetCloudsMax()
    {
        Location MaxLocation = GetMaxLocation();
        Vector2Int MaxTileLocation = MaxLocation.GlobalTileLocation;
        return new(
            MaxTileLocation.x * TileSize.x * 2 + TileSize.x,
            TileSize.y + 0.6f,
            MaxTileLocation.y * TileSize.z * 2 + TileSize.z
        );
    }

    static Vector3 Div(Vector3 A, Vector3 B)
    {
        return new Vector3(A.x / B.x, A.y / B.y, A.z / B.z);
    }

    static Vector2 RayBoxDist(Vector3 RayOrigin, Vector3 RayDir)
    {
        // adapted from sebastian lague
        // slightly extend the box since the hexagons are center positioned
        Vector3 BoundsMin = GetCloudsMin();
        Vector3 BoundsMax = GetCloudsMax();

        Vector3 T0 = Div((BoundsMin - RayOrigin), RayDir);
        Vector3 T1 = Div((BoundsMax - RayOrigin), RayDir);
        Vector3 TMin = Vector3.Min(T0, T1);
        Vector3 TMax = Vector3.Max(T0, T1);

        float DistA = Mathf.Max(Mathf.Max(TMin.x, TMin.y), TMin.z); ;
        float DistB = Mathf.Min(TMax.x, Mathf.Min(TMax.y, TMax.z));

        float DistToBox = Mathf.Max(0, DistA);
        float DistInsideBox = Mathf.Max(0, DistB - DistToBox);
        return new Vector3(DistToBox, DistInsideBox);
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

    public static bool IsValidLocation(Location Location)
    {
        return IsValidChunkIndex(Location.ChunkLocation) && IsValidHexIndex(Location.HexLocation);
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

    public static Vector3 GetPointOnPlane(Vector3 PlaneOrigin, Vector3 PlaneNormal, Vector3 Origin, Vector3 Direction)
    {
        float A = Vector3.Dot(PlaneOrigin - Origin, PlaneNormal);
        float B = Vector3.Dot(Direction, PlaneNormal);
        float d = A / B;
        return Origin + Direction * d;
    }

    public static int GetCostsFromTo(Location locationA, Location locationB, bool bTakeRawData = false)
    {
        if (!Game.TryGetService(out MapGenerator MapGenerator))
            return -1;

        if (!MapGenerator.TryGetHexagonData(locationA, out HexagonData DataA, bTakeRawData))
            return -1;

        if (!MapGenerator.TryGetHexagonData(locationB, out HexagonData DataB, bTakeRawData))
            return -1;

        if (DataB.IsMalaised())
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

    public static int GetSetBitsAmount(int Category)
    {
        Category = Category - ((Category >> 1) & 0x55555555);
        Category = (Category & 0x33333333) + ((Category >> 2) & 0x33333333);
        int BitAmount = ((Category + (Category >> 4) & 0xF0F0F0F) * 0x1010101) >> 24;
        return BitAmount;
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
