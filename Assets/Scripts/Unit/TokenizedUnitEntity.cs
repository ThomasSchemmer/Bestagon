using System;
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
    public abstract string GetName();
    protected abstract bool TryGetMovementAttribute(out AttributeType Type);

    public int GetMovementCostTo(Location ToLocation)
    {
        var Params = GetPathfindingParams();
        List<Location> Path = Pathfinding.FindPathFromTo(Location, ToLocation, Params);
        return Pathfinding.GetCostsForPath(Path, Params);
    }

    public void MoveTo(Location Location, int Costs)
    {
        List<Location> Path = Pathfinding.FindPathFromTo(this.Location, Location, GetPathfindingParams());

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
        RemainingMovement = TryGetMovementAttribute(out var Type) ?
            (int)PlayerAttributes[Type].CurrentValue :
            MovementPerTurn;
    }

    public abstract Vector3 GetOffset();

    public abstract Quaternion GetRotation();

    public abstract Pathfinding.Parameters GetPathfindingParams();

    public static bool _IsInteractableWith(HexagonVisualization Hex, bool bIsPreview)
    {

        if (Hex.Data.GetDiscoveryState() != HexagonData.DiscoveryState.Visited)
        {
            if (!bIsPreview)
            {
                MessageSystemScreen.CreateMessage(Message.Type.Error, "Can only place on scouted tiles");
            }
            return false;
        }

        if (Hex.Data.IsMalaised())
        {
            if (!bIsPreview)
            {
                MessageSystemScreen.CreateMessage(Message.Type.Error, "Cannot place on corrupted tiles");
            }
            return false;
        }
        return true; 
    }

    public override bool TryInteractWith(HexagonVisualization Hex)
    {
        if (!Game.TryGetServices(out Units Units, out MapGenerator MapGenerator))
            return false;

        if (!IsInteractableWith(Hex, false))
            return false;

        if (Units.IsEntityAt(Hex.Location))
        {
            MessageSystemScreen.CreateMessage(Message.Type.Error, "Cannot create unit here - one already exists");
            return false;
        }

        return true;
    }

    public bool IsInteractableWith(HexagonVisualization Hex, bool bIsPreview)
    {
        if (this is BoatEntity)
            return BoatEntity._IsInteractableWith(Hex, bIsPreview);

        if (this is ScoutEntity)
            return ScoutEntity._IsInteractableWith(Hex, bIsPreview);

        throw new NotImplementedException("Every subclass needs its own entry here");
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

    public LocationSet GetLocations()
    {
        return Location.ToSet();
    }
    
    public void SetLocation(LocationSet Location)
    {
        this.Location = Location.GetMainLocation();
    }

    public override bool IsAboutToBeMalaised()
    {
        if (!Game.TryGetService(out MapGenerator MapGenerator))
            return false;

        if (!MapGenerator.TryGetHexagonData(Location, out HexagonData Hex))
            return false;

        return Hex.IsPreMalaised();
    }
    public override bool IsIdle()
    {
        return RemainingMovement > 0;
    }

    public UType GetUType()
    {
        return UnitType;
    }

    [SaveableBaseType]
    public int MovementPerTurn = 1;
    [HideInInspector]
    [SaveableBaseType]
    public int RemainingMovement = 0;

    [HideInInspector]
    [SaveableClass]
    // units can only be at one location at a time, so keep simple
    protected Location Location;

    [SaveableBaseType]
    // should always be greater than the MovementPerTurn!
    public int VisitingRange = 3;
    [SaveableBaseType]
    public int ScoutingRange = 4;

    // don't save, its only temporarily constructed anyway
    [HideInInspector]
    public UnitVisualization Visualization;

    public delegate void OnMovementTo(Location Location);
    public delegate void OnMovement(int Count);
    public static event OnMovementTo _OnMovementTo;
    public static event OnMovement _OnMovement;
}
