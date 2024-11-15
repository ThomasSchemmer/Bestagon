using static HexagonConfig;
using Unity.Collections;
using static HexagonData;
using System.Collections.Generic;
using System;
using System.Numerics;

/** Includes all data necessary to display and update a hexagon */
public class HexagonData : ISaveableData
{
    public enum DiscoveryState
    {
        Unknown = 0,    // never been close -> invisible
        Scouted = 1,    // been close -> only as outline
        Visited = 2     // been very close -> visible
    }

    [Flags]
    /** 
     * Describes the current interaction state of the hexagon (and its Visualization) 
     * Needs to be kept in sync with @HexagonShader
     */
    public enum State : uint
    {
        Default = 0,
        Hovered = 1 << 0,
        Selected = 1 << 1,
        PreMalaised = 1 << 2,
        Malaised = 1 << 3,
        // fully highlighted, next to a building
        Adjacent = 1 << 4,
        // fully highlighted, neext to a unit
        Reachable = 1 << 5,
        // only outer rim will be highlighted, needs to have a @AoESource set!
        AoeAffected = 1 << 6,
    }

    public Location Location;
    public HexagonType Type;
    public HexagonHeight HexHeight;

    // only for visual highlights
    private Location AoESource = Location.Invalid;

    private State _State = State.Default;
    private DiscoveryState Discovery = DiscoveryState.Unknown;

    public delegate void OnDiscovery();
    public delegate void OnDiscoveryStateHex(HexagonData Data, DiscoveryState State);
    public OnDiscovery _OnDiscovery;
    public static OnDiscoveryStateHex _OnDiscoveryStateHex;

    // copied/filled from HexagonInfo struct
    private float Temperature;
    private float Humidity;
    private float Height;

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
        uint Malaise = (uint)(GetState(State.Malaised) ? 1 : 0) << 7;
        uint uDiscovery = (uint)(Discovery > DiscoveryState.Unknown ? 1 : 0) << 6;

        return new HexagonDTO() {
            Type = uType + Malaise + uDiscovery,
        };
    }

    public DiscoveryState GetDiscoveryState()
    {
        return Discovery;
    }

    public void UpdateDiscoveryState(DiscoveryState NewState, bool bForce = false)
    {
        if (NewState <= Discovery && !bForce)
            return;

        Discovery = NewState;

        // needs to be called before OnDiscovery to update HexData before mesh creation
        _OnDiscoveryStateHex?.Invoke(this, Discovery);
        _OnDiscovery?.Invoke();

        if (!Game.TryGetService(out MapGenerator MapGenerator))
            return;
        MapGenerator.InvokeDiscovery(Discovery);
    }

    public int GetSize()
    {
        // Height, discovery and malaise each get a byte, type cant be smaller than int
        return Location.GetStaticSize() + 3 + sizeof(double) * 3 + sizeof(int);
    }

    public bool IsMalaised()
    {
        return GetState(State.Malaised);
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
        return GetState(State.PreMalaised);
    }
    

    public bool CanDecorationSpawn()
    {
        return HexHeight > HexagonHeight.Sea && HexHeight < HexagonHeight.Mountain;
    }

    public void AddState(State State)
    {
        _State |= State;
    }

    public void RemoveState(State State)
    {
        _State &= ~State;
    }

    public void SetState(State State, bool bIsAdded)
    {
        if (bIsAdded)
        {
            AddState(State);
            return;
        }
        RemoveState(State);
    }

    public bool GetState(State State)
    {
        return (_State & State) > 0;
    }

    public State GetState()
    {
        return _State;
    }

    public void SetMalaised()
    {
        SetState(State.PreMalaised, false);
        SetState(State.Malaised, true);
    }

    public void SetPreMalaised()
    {
        SetState(State.PreMalaised, true);
        SetState(State.Malaised, false);
    }

    public void RemoveMalaise()
    {
        SetState(State.PreMalaised, false);
        SetState(State.Malaised, false);
    }

    public bool IsAnyMalaised()
    {
        return GetState(State.Malaised) || GetState(State.PreMalaised);
    }

    public UnityEngine.Vector4 GetSourceLocationVector()
    {
        return new(
            Location.WorldLocation.x,
            Location.WorldLocation.z,
            AoESource.WorldLocation.x,
            AoESource.WorldLocation.z
        );
    }

    public void SetAoESourceLocation(Location Location)
    {
        bool bHasValidSource = !Location.Equals(Location.Invalid);
        SetState(State.AoeAffected, bHasValidSource);
        AoESource = Location;
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
            Type = TempType,
        };
    }

    public static HexagonData Create(HexagonHeight Height, HexagonType Type)
    {
        return new(Height, Type);
    }

    public byte[] GetData()
    {
        NativeArray<byte> Bytes = new(GetSize(), Allocator.Temp);
        int Pos = 0;
        Pos = SaveGameManager.AddSaveable(Bytes, Pos, Location);
        Pos = SaveGameManager.AddEnumAsByte(Bytes, Pos, (byte)HexHeight);
        Pos = SaveGameManager.AddEnumAsByte(Bytes, Pos, (byte)Discovery);
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
        Pos = SaveGameManager.GetInt(Bytes, Pos, out int iType);
        Pos = SaveGameManager.GetBool(Bytes, Pos, out bool bIsMalaised);
        Pos = SaveGameManager.GetDouble(Bytes, Pos, out double dHeight);
        Pos = SaveGameManager.GetDouble(Bytes, Pos, out double dTemperature);
        Pos = SaveGameManager.GetDouble(Bytes, Pos, out double dHumidity);

        Type = (HexagonType)iType;
        HexHeight = (HexagonHeight)iHeight;
        Discovery = (DiscoveryState)iDiscovery;
        Height = (float)dHeight;
        Temperature = (float)dTemperature;
        Humidity = (float)dHumidity;

        SetState(State.Malaised, bIsMalaised);
    }
}
