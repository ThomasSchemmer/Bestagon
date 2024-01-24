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
    public HexagonHeight HexHeight;
    public bool bIsMalaised;

    // copied from HexagonInfo struct
    public float Temperature;
    public float Humidity;
    public float Height;

    public float DebugValue;

    public HexagonData(HexagonHeight HexHeight, HexagonType HexType)
    {
        this.HexHeight = HexHeight;
        this.Type = HexType;
    }

    public HexagonData() { }

    public float WorldHeight
    {
        get { return GetWorldHeightFromTile(new(HexHeight, Type)); }
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
        return Location.GetStaticSize() + 3 + sizeof(double) * 3;
    }

    public static HexagonData CreateFromInfo(WorldGenerator.HexagonInfo Info)
    {
        HexagonType TempType = (HexagonType)IntToMask((int)Info.TypeIndex);
        return new()
        {
            Height = Info.Height,
            Humidity = Info.Humidity,
            Temperature = Info.Temperature,
            HexHeight = (HexagonHeight)Info.HexHeightIndex,
            Type = (HexagonType)IntToMask((int)Info.TypeIndex),
        };
    }

    public byte[] GetData()
    {
        NativeArray<byte> Bytes = new(GetSize(), Allocator.Temp);
        int Pos = 0;
        Pos = SaveGameManager.AddSaveable(Bytes, Pos, Location);
        Pos = SaveGameManager.AddEnumAsInt(Bytes, Pos, (int)Type);
        Pos = SaveGameManager.AddEnumAsInt(Bytes, Pos, (int)HexHeight);
        Pos = SaveGameManager.AddBool(Bytes, Pos, bIsMalaised);
        Pos = SaveGameManager.AddDouble(Bytes, Pos, Height);
        Pos = SaveGameManager.AddDouble(Bytes, Pos, Temperature);
        Pos = SaveGameManager.AddDouble(Bytes, Pos, Humidity);

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
        Pos = SaveGameManager.GetDouble(Bytes, Pos, out double dHeight);
        Pos = SaveGameManager.GetDouble(Bytes, Pos, out double dTemperature);
        Pos = SaveGameManager.GetDouble(Bytes, Pos, out double dHumidity);

        Type = (HexagonType)iType;
        HexHeight = (HexagonHeight)iHeight;
        Height = (float)dHeight;
        Temperature = (float)dTemperature;
        Humidity = (float)dHumidity;
    }
}
