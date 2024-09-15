using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Assertions;

/** 
 * Any unit that is visually represented by an in-game token. Also supports moving this token
 * Still only contains data and is not directly linked to the token itself ("MonoBehaviour"), see 
 * @UnitVisualization for that. During lifetime will be managed by @Units and only exists there
 */
public abstract class TokenizedUnitEntity : StarvableUnitEntity, IPreviewable, ITokenized
{
    public abstract string GetPrefabName();

    public int GetMovementCostTo(Location ToLocation)
    {
        List<Location> Path = Pathfinding.FindPathFromTo(Location, ToLocation);
        return Pathfinding.GetCostsForPath(Path);
    }

    public void MoveTo(Location Location, int Costs)
    {
        List<Location> Path = Pathfinding.FindPathFromTo(this.Location, Location);

        Location OldLocation = this.Location;
        this.Location = Location;
        this.RemainingMovement -= Costs;

        _OnMovementTo?.Invoke(Location);
        _OnMovement?.Invoke(Path.Count);

        if (!Game.TryGetService(out Units Units))
            return;
        Units.InvokeUnitMoved(this);

        if (!Game.TryGetService(out MapGenerator MapGenerator))
            return;

        if (!MapGenerator.TryGetHexagon(Location, out HexagonVisualization NewHex))
            return;

        // movement modifiers might be larger than set vis/scout range, leading to "move to invis"
        int MovementRange = (int)AttributeSet.Get()[AttributeType.ScoutMovementRange].CurrentValue;
        int ScoutDiff = ScoutingRange - VisitingRange;
        int MaxVisRange = Mathf.Max(VisitingRange, MovementRange);
        int MaxScoutRange = Mathf.Max(ScoutingRange, MaxVisRange + ScoutDiff);
        NewHex.UpdateDiscoveryState(MaxVisRange, MaxScoutRange);

        if (!MapGenerator.TryGetChunkVis(OldLocation, out ChunkVisualization OldVis))
            return;
        
        OldVis.RefreshTokens();

        if (!MapGenerator.TryGetChunkVis(Location, out ChunkVisualization ChunkVis))
            return;

        ChunkVis.RefreshTokens();
    }

    public virtual Production GetMovementRequirements()
    {
        return Production.Empty;
    }

    public override void Refresh()
    {
        if (IsStarving(false))
            return;

        AttributeSet PlayerAttributes = AttributeSet.Get();
        if (PlayerAttributes == null)
            return;

        base.Refresh();
        RemainingMovement = (int)PlayerAttributes[AttributeType.ScoutMovementRange].CurrentValue;
    }

    public abstract Vector3 GetOffset();

    public abstract Quaternion GetRotation();

    public abstract bool IsPreviewInteractableWith(HexagonVisualization Hex, bool bIsPreview);

    public override bool TryInteractWith(HexagonVisualization Hex)
    {
        if (!Game.TryGetServices(out Units Units, out MapGenerator MapGenerator))
            return false;

        if (!IsPreviewInteractableWith(Hex, false))
            return false;

        if (Units.IsEntityAt(Hex.Location))
        {
            MessageSystemScreen.CreateMessage(Message.Type.Error, "Cannot create unit here - one already exists");
            return false;
        }

        Init();
        Units.AddUnit(this);

        MoveTo(Hex.Location, 0);

        Refresh();
        return true;
    }

    public void SetVisualization(EntityVisualization Vis)
    {
        if (Vis is not UnitVisualization)
            return;

        Visualization = Vis as UnitVisualization;
    }

    public bool HasRemainingMovement()
    {
        return RemainingMovement > 0;
    }

    public override int GetSize()
    {
        return GetStaticSize();
    }

    public static new int GetStaticSize()
    {
        // 4 bytes, one each for MovementPerTurn, RemainingMovement, VisitingRange, ScoutingRange,
        return StarvableUnitEntity.GetStaticSize() + 4 * sizeof(byte) + Location.GetStaticSize();
    }

    public override byte[] GetData()
    {
        // necessary as ScoutData overrides GetSize, forcing this Data to be too large for its own data
        int TotalSize = GetStaticSize();
        NativeArray<byte> Bytes = SaveGameManager.GetArrayWithBaseFilled(TotalSize, base.GetSize(), base.GetData());

        int Pos = base.GetSize();
        Pos = SaveGameManager.AddByte(Bytes, Pos, (byte)MovementPerTurn);
        Pos = SaveGameManager.AddByte(Bytes, Pos, (byte)RemainingMovement);
        Pos = SaveGameManager.AddByte(Bytes, Pos, (byte)VisitingRange);
        Pos = SaveGameManager.AddByte(Bytes, Pos, (byte)ScoutingRange);
        Pos = SaveGameManager.AddSaveable(Bytes, Pos, Location);

        return Bytes.ToArray();
    }

    public override void SetData(NativeArray<byte> Bytes)
    {
        base.SetData(Bytes);
        int Pos = base.GetSize();
        Pos = SaveGameManager.GetByte(Bytes, Pos, out byte bMovementPerTurn);
        Pos = SaveGameManager.GetByte(Bytes, Pos, out byte bRemainingMovement);
        Pos = SaveGameManager.GetByte(Bytes, Pos, out byte bVisitingRange);
        Pos = SaveGameManager.GetByte(Bytes, Pos, out byte bScoutingRage);
        Pos = SaveGameManager.SetSaveable(Bytes, Pos, Location);

        MovementPerTurn = bMovementPerTurn;
        RemainingMovement = bRemainingMovement;
        VisitingRange = bVisitingRange;
        ScoutingRange = bScoutingRage;
    }

    public Location GetLocation()
    {
        return Location;
    }
    
    public void SetLocation(Location Location)
    {
        this.Location = Location;
    }

    public int MovementPerTurn = 1;
    [HideInInspector]
    public int RemainingMovement = 0;
    [HideInInspector]
    protected Location Location;

    // should always be greater than the MovementPerTurn!
    public int VisitingRange = 3;
    public int ScoutingRange = 4;

    // don't save, its only temporarily constructed anyway
    [HideInInspector]
    public UnitVisualization Visualization;

    public delegate void OnMovementTo(Location Location);
    public delegate void OnMovement(int Count);
    public static event OnMovementTo _OnMovementTo;
    public static event OnMovement _OnMovement;
}
