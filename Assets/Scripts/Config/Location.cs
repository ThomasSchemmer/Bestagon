using System;
using UnityEngine;
[Serializable]
public class Location 
{
    public Location(Vector2Int ChunkLocation, Vector2Int HexLocation) {
        _ChunkLocation = ChunkLocation;
        _HexLocation = HexLocation;
    }

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
}
