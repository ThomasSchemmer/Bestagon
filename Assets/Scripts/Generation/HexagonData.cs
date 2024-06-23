using static HexagonConfig;
using Unity.Collections;

/** Includes all data necessary to display and update a hexagon */
public class HexagonData : ISaveable
{
    public enum DiscoveryState
    {
        Unknown = 0,    // never been close -> invisible
        Scouted = 1,    // been close -> only as outline
        Visited = 2     // been very close -> visible
    }

    public enum MalaiseState
    {
        None,
        PreMalaise,
        Malaised
    }

    public Location Location;
    public HexagonType Type;
    public HexagonDecoration Decoration;
    public HexagonHeight HexHeight;
    public MalaiseState MalaisedState = MalaiseState.None;
    private DiscoveryState Discovery = DiscoveryState.Unknown;

    public delegate void OnDiscovery();
    public OnDiscovery _OnDiscovery;

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
        uint Malaise = (uint)(MalaisedState == MalaiseState.Malaised ? 1 : 0) << 7;
        uint uDiscovery = (uint)(Discovery > DiscoveryState.Unknown ? 1 : 0) << 6;

        return new HexagonDTO() {
            Type = uType + Malaise + uDiscovery,
        };
    }

    public DiscoveryState GetDiscoveryState()
    {
        return Discovery;
    }

    public void UpdateDiscoveryState(DiscoveryState NewState)
    {
        if (NewState < Discovery)
            return;

        Discovery = NewState;
        _OnDiscovery?.Invoke();
    }

    public int GetSize()
    {
        // Height, discovery, ruins and malaise each get a byte, type cant be smaller than int
        return Location.GetStaticSize() + 4 + sizeof(double) * 3 + sizeof(int);
    }

    public bool IsMalaised()
    {
        return MalaisedState == MalaiseState.Malaised;
    }

    public bool IsScouted()
    {
        return Discovery >= DiscoveryState.Scouted;
    }

    public bool IsVisited()
    {
        return Discovery >= DiscoveryState.Visited;
    }

    public bool IsPreMalaised()
    {
        return MalaisedState == MalaiseState.PreMalaise;
    }

    public string GetDecorationText()
    {
        switch (Decoration)
        {
            case HexagonDecoration.Ruins: return "Contains ancient ruins";
            case HexagonDecoration.Tribe: return "Contains unknown tribe";
            default: return "";
        }
    }

    public static HexagonData CreateFromInfo(WorldGenerator.HexagonInfo Info)
    {
        HexagonType TempType = (HexagonType)IntToMask(MaskToInt((int)Info.TypeIndex, 32));

        return new()
        {
            Height = Info.Height,
            Humidity = Info.Humidity,
            Temperature = Info.Temperature,
            HexHeight = (HexagonHeight)Info.HexHeightIndex,
            Decoration = (HexagonDecoration)Info.DecorationIndex,
            Type = TempType,
        };
    }

    public byte[] GetData()
    {
        NativeArray<byte> Bytes = new(GetSize(), Allocator.Temp);
        int Pos = 0;
        Pos = SaveGameManager.AddSaveable(Bytes, Pos, Location);
        Pos = SaveGameManager.AddEnumAsByte(Bytes, Pos, (byte)HexHeight);
        Pos = SaveGameManager.AddEnumAsByte(Bytes, Pos, (byte)Discovery);
        Pos = SaveGameManager.AddEnumAsByte(Bytes, Pos, (byte)Decoration);
        Pos = SaveGameManager.AddInt(Bytes, Pos, (int)Type);
        Pos = SaveGameManager.AddBool(Bytes, Pos, IsMalaised());
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
        Pos = SaveGameManager.GetEnumAsByte(Bytes, Pos, out byte iHeight);
        Pos = SaveGameManager.GetEnumAsByte(Bytes, Pos, out byte iDiscovery);
        Pos = SaveGameManager.GetEnumAsByte(Bytes, Pos, out byte iDecoration);
        Pos = SaveGameManager.GetInt(Bytes, Pos, out int iType);
        Pos = SaveGameManager.GetBool(Bytes, Pos, out bool bIsMalaised);
        Pos = SaveGameManager.GetDouble(Bytes, Pos, out double dHeight);
        Pos = SaveGameManager.GetDouble(Bytes, Pos, out double dTemperature);
        Pos = SaveGameManager.GetDouble(Bytes, Pos, out double dHumidity);

        Type = (HexagonType)iType;
        HexHeight = (HexagonHeight)iHeight;
        Discovery = (DiscoveryState)iDiscovery;
        Decoration = (HexagonDecoration)iDecoration;
        MalaisedState = bIsMalaised ? MalaiseState.Malaised : MalaiseState.None;
        Height = (float)dHeight;
        Temperature = (float)dTemperature;
        Humidity = (float)dHumidity;
    }
}
