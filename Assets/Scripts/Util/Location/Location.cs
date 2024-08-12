using System;
using Unity.Collections;
using UnityEngine;

[Serializable]
public class Location : ISaveableData
{
    public Location(Vector2Int ChunkLocation, Vector2Int HexLocation) {
        _ChunkLocation = new (ChunkLocation.x, ChunkLocation.y);
        _HexLocation = new (HexLocation.x, HexLocation.y);
    }

    public Location(int v1, int v2, int v3, int v4)
    {
        _ChunkLocation = new(v1, v2);
        _HexLocation = new(v3, v4);
    }

    public Location() : this(0, 0, 0, 0) {}

    public Vector2Int GlobalTileLocation{
        get {
            return HexagonConfig.ChunkSpaceToTileSpace(ChunkLocation) + HexLocation;
        }
    }

    public Vector2Int HexLocation {
        get {
            return new(_HexLocation.x, _HexLocation.y);
        }
    }

    public Vector2Int ChunkLocation {
        get {
            return new(_ChunkLocation.x, _ChunkLocation.y);
        }
    }

    public Vector3 WorldLocation {
        get {
            return HexagonConfig.TileSpaceToWorldSpace(GlobalTileLocation);
        }
    }

    public Location Copy() {
        return new Location(ChunkLocation, HexLocation);
    }

    public int DistanceTo(Location Other) {
        return Mathf.Abs(Other.GlobalTileLocation.x - GlobalTileLocation.x) + Mathf.Abs(Other.GlobalTileLocation.y - GlobalTileLocation.y);
    }

    public Vector4 ToVec4() {
        return new Vector4(ChunkLocation.x, ChunkLocation.y, HexLocation.x, HexLocation.y);
    }

    public Location GetMaxLocationInChunk()
    {
        return new Location(ChunkLocation.x, ChunkLocation.y, HexagonConfig.ChunkSize - 1, HexagonConfig.ChunkSize - 1);
    }

    public Location GetMinLocationInChunk()
    {
        return new Location(ChunkLocation.x, ChunkLocation.y, 0, 0);
    }

    public Location GetMirrorLocation(bool IsMin)
    {
        int Chunk = IsMin ? Mathf.Min(ChunkLocation.x, ChunkLocation.y) : Mathf.Max(ChunkLocation.x, ChunkLocation.y);
        int Hex = IsMin ? Mathf.Min(HexLocation.x, HexLocation.y) : Mathf.Max(HexLocation.x, HexLocation.y);
        return new(Chunk, Chunk, Hex, Hex);
    }

    public override string ToString() {
        return "("+ChunkLocation.x+", "+ChunkLocation.y+") - ("+HexLocation.x+", "+HexLocation.y+")";
    }

    public override bool Equals(object obj) {
        if (obj is not Location)
            return false;

        Location Other = obj as Location;
        return ChunkLocation.x == Other.ChunkLocation.x &&
            ChunkLocation.y == Other.ChunkLocation.y &&
            HexLocation.x == Other.HexLocation.x &&
            HexLocation.y == Other.HexLocation.y;
    }

    public override int GetHashCode() {
        return HashCode.Combine(ChunkLocation.GetHashCode(), HexLocation.GetHashCode());
    }

    [SerializeField]
    protected SerializedVector2<int> _ChunkLocation;
    [SerializeField]
    protected SerializedVector2<int> _HexLocation;

    public static Location Zero {
        get {
            return CreateChunk(0, 0);
        }
    }

    public static Location MinValue {
        get {
            return CreateChunk(int.MinValue, int.MinValue);
        }
    }

    public static Location operator+(Location A, Location B) {
        HexagonConfig.GlobalTileToChunkAndTileSpace(A.GlobalTileLocation + B.GlobalTileLocation, out Location AB);
        return AB;
    }
    public static Location operator -(Location A, Location B) {
        HexagonConfig.GlobalTileToChunkAndTileSpace(A.GlobalTileLocation - B.GlobalTileLocation, out Location AB);
        return AB;
    }

    public static Location CreateHex(Vector2Int HexLocation) {
        return new Location(new Vector2Int(0, 0), HexLocation);
    }

    public static Location CreateHex(int x, int y) {
        return new Location(new Vector2Int(0, 0), new Vector2Int(x, y));
    }

    public static Location CreateChunk(Vector2Int ChunkLocation) {
        return new Location(ChunkLocation, new Vector2Int(0, 0));
    }

    public static Location CreateChunk(int x, int y) {
        return new Location(new Vector2Int(x, y), new Vector2Int(0, 0));
    }

    public static Location Min(Location A, Location B) {
        return new Location(Mathf.Min(A.ChunkLocation.x, B.ChunkLocation.x),
            Mathf.Min(A.ChunkLocation.y, B.ChunkLocation.y),
            Mathf.Min(A.HexLocation.x, B.HexLocation.x),
            Mathf.Min(A.HexLocation.y, B.HexLocation.y)
        );
    }
    public static Location Max(Location A, Location B) {
        return new Location(Mathf.Max(A.ChunkLocation.x, B.ChunkLocation.x),
            Mathf.Max(A.ChunkLocation.y, B.ChunkLocation.y),
            Mathf.Max(A.HexLocation.x, B.HexLocation.x),
            Mathf.Max(A.HexLocation.y, B.HexLocation.y)
        );
    }

    public static Location CreateFromVector(Vector4 Vec)
    {
        return new((int)Vec.x, (int)Vec.y, (int)Vec.z, (int)Vec.w);
    }

    public int GetSize()
    {
        return GetStaticSize();
    }

    public static int GetStaticSize()
    {
        return sizeof(int) * 4;
    }

    public byte[] GetData()
    {
        NativeArray<byte> Bytes = new(GetSize(), Allocator.Temp);
        int Pos = 0;
        Pos = SaveGameManager.AddInt(Bytes, Pos, _ChunkLocation.x);
        Pos = SaveGameManager.AddInt(Bytes, Pos, _ChunkLocation.y);
        Pos = SaveGameManager.AddInt(Bytes, Pos, _HexLocation.x);
        Pos = SaveGameManager.AddInt(Bytes, Pos, _HexLocation.y);

        return Bytes.ToArray();
    }

    public void SetData(NativeArray<byte> Bytes)
    {
        int Pos = 0;
        Pos = SaveGameManager.GetInt(Bytes, Pos, out int CX);
        Pos = SaveGameManager.GetInt(Bytes, Pos, out int CY);
        Pos = SaveGameManager.GetInt(Bytes, Pos, out int HX);
        Pos = SaveGameManager.GetInt(Bytes, Pos, out int HY);
        _ChunkLocation = new (CX, CY);
        _HexLocation = new (HX, HY);
    }
}
