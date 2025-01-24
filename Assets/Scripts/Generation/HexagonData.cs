using static HexagonConfig;
using Unity.Collections;
using static HexagonData;
using System.Collections.Generic;
using System;
using System.Numerics;

/** Includes all data necessary to display and update a hexagon */
public class HexagonData
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

    [SaveableClass]
    public Location Location;
    [SaveableEnum]
    public HexagonType Type;
    [SaveableEnum]
    public HexagonHeight HexHeight;

    // only for visual highlights
    private Location AoESource = Location.Invalid;

    [SaveableEnum]
    private State _State = State.Default;
    [SaveableEnum]
    private DiscoveryState Discovery = DiscoveryState.Unknown;

    public delegate void OnDiscovery();
    public delegate void OnHexDiscoveryState(HexagonData Data, DiscoveryState State);
    public delegate void OnHexMalaised(HexagonData Data);
    public OnDiscovery _OnDiscovery;
    public static OnHexDiscoveryState _OnHexDiscoveryState;
    public static OnHexMalaised _OnHexMalaised;

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
        _OnHexDiscoveryState?.Invoke(this, Discovery);
        _OnDiscovery?.Invoke();

        if (!Game.TryGetService(out MapGenerator MapGenerator))
            return;
        MapGenerator.InvokeDiscovery(Discovery);
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
        _OnHexMalaised?.Invoke(this);
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
            HexHeight = (HexagonHeight)Info.HexHeightIndex,
            Type = TempType,
        };
    }

    public static HexagonData Create(HexagonHeight Height, HexagonType Type)
    {
        return new(Height, Type);
    }

    public override int GetHashCode()
    {
        return Location.GetHashCode();
    }
}
