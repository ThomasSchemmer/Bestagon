using System;
using System.Drawing;
using Unity.Collections;
using UnityEngine;
using UnityEngine.XR;
[Serializable]
public class Location : ISaveable
{
    public Location(Vector2Int ChunkLocation, Vector2Int HexLocation) {
        _ChunkLocation = ChunkLocation;
        _HexLocation = HexLocation;
    }

    public Location(int v1, int v2, int v3, int v4) : 
        this(new Vector2Int(v1, v2), new Vector2Int(v3, v4)) 
        { }

    public Vector2Int GlobalTileLocation{
        get {
            return HexagonConfig.ChunkSpaceToTileSpace(_ChunkLocation) + _HexLocation;
        }
    }

    public Vector2Int HexLocation {
        get {
            return _HexLocation;
        }
    }

    public Vector2Int ChunkLocation {
        get {
            return _ChunkLocation;
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

    private Vector2Int _ChunkLocation;
    private Vector2Int _HexLocation;

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

    public int GetSize()
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

    public void SetData(byte[] Data)
    {
        NativeArray<byte> Bytes = new(Data, Allocator.Temp);
        NativeSlice<byte> Slice;
        Slice = new NativeSlice<byte>(Bytes, 0, 4);
        int CX = BitConverter.ToInt32(Slice.ToArray());
        Slice = new NativeSlice<byte>(Bytes, 4, 4);
        int CY = BitConverter.ToInt32(Slice.ToArray());
        Slice = new NativeSlice<byte>(Bytes, 8, 4);
        int HX = BitConverter.ToInt32(Slice.ToArray());
        Slice = new NativeSlice<byte>(Bytes, 12, 4);
        int HY = BitConverter.ToInt32(Slice.ToArray());
        _ChunkLocation = new Vector2Int(CX, CY);
        _HexLocation = new Vector2Int(HX, HY);
    }
}
