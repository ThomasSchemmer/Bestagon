using System.Diagnostics;
using Unity.Mathematics;
using static Map;
using static HexagonConfig;
using System;
using Unity.Collections;

/** Includes all data necessary to display and update a hexagon */
public class HexagonData : ISaveable
{
    public Location Location;
    public HexagonType Type;
    public HexagonHeight Height;
    public bool bIsMalaised;
    public float Value;
    public float WorldHeight
    {
        get { return GetWorldHeightFromTile(new(Height, Type)); }
    }

    /** 
     * Converts the data into a transferable, lightweight object. 
     * Only contains data necessary for the minimap
     */
    public HexagonDTO GetDTO() {

        uint uType = (uint)MaskToInt((int)Type, 16) + 1;
        uint Malaise = (uint)(bIsMalaised ? 1 : 0) << 7;

        return new HexagonDTO() {
            Type = uType + Malaise,
        };
    }

    public int GetSize()
    {
        // Type, Height and malaise each get a byte
        return Location.GetStaticSize() + 3 + sizeof(double);
    }

    public byte[] GetData()
    {
        NativeArray<byte> Bytes = new(GetSize(), Allocator.Temp);
        int Pos = 0;
        Pos = SaveGameManager.AddSaveable(Bytes, Pos, Location);
        Pos = SaveGameManager.AddEnumAsInt(Bytes, Pos, (int)Type);
        Pos = SaveGameManager.AddEnumAsInt(Bytes, Pos, (int)Height);
        Pos = SaveGameManager.AddBool(Bytes, Pos, bIsMalaised);
        Pos = SaveGameManager.AddDouble(Bytes, Pos, Value);

        return Bytes.ToArray();
    }

    public void SetData(NativeArray<byte> Bytes)
    {
        Location = Location.Zero;

        int Pos = 0;
        Pos = SaveGameManager.SetSaveable(Bytes, Pos, Location);
        Pos = SaveGameManager.GetEnumAsInt(Bytes, Pos, out int iType);
        Pos = SaveGameManager.GetEnumAsInt(Bytes, Pos, out int iHeight);
        Pos = SaveGameManager.GetBool(Bytes, Pos, out bIsMalaised);
        Pos = SaveGameManager.GetDouble(Bytes, Pos, out double dValue);

        Type = (HexagonType)iType;
        Height = (HexagonHeight)iHeight;
        Value = (float)dValue;
    }
}
