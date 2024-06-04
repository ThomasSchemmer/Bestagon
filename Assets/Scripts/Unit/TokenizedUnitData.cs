using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Assertions;
using static UnityEditor.FilePathAttribute;
using static UnityEngine.UI.CanvasScaler;

/** 
 * Any unit that is represented by an in-game token. Also supports moving this token
 * Still only contains data and is not directly linked to the token itself ("MonoBehaviour"), see 
 * @UnitVisualization for that. During lifetime will be managed by @Units and only exists there
 */
public abstract class TokenizedUnitData : StarvableUnitData, IPreviewable
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

        if (!Game.TryGetService(out MapGenerator MapGenerator))
            return;

        if (!MapGenerator.TryGetHexagon(Location, out HexagonVisualization NewHex))
            return;

        NewHex.UpdateDiscoveryState(VisitingRange, ScoutingRange);

        if (MapGenerator.TryGetChunkData(OldLocation, out ChunkData OldChunk) && OldChunk.Visualization)
        {
            OldChunk.Visualization.RefreshTokens();
        }

        if (MapGenerator.TryGetChunkData(Location, out ChunkData Chunk) && Chunk.Visualization)
        {
            Chunk.Visualization.RefreshTokens();
        }
    }

    public virtual Production GetMovementRequirements()
    {
        return Production.Empty;
    }

    public override void Refresh()
    {
        if (IsStarving())
            return;

        base.Refresh();
        RemainingMovement = MovementPerTurn;
    }

    public void MoveAlong(List<Location> Path)
    {
        Assert.AreNotEqual(Path.Count, 0);
        Assert.AreEqual(Path[0], Location);
        for (int i = 1; i < Path.Count; i++)
        {
            Location CurrentLocation = this.Location;
            Location NextLocation = Path[i];
            int MoveCosts = HexagonConfig.GetCostsFromTo(CurrentLocation, NextLocation);
            if (RemainingMovement < MoveCosts)
                break;

            MoveTo(NextLocation, MoveCosts);
        }
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

        if (Units.IsUnitAt(Hex.Location))
        {
            MessageSystem.CreateMessage(Message.Type.Error, "Cannot create unit here - one already exists");
            return false;
        }

        Init();
        Units.AddUnit(this);

        MoveTo(Hex.Location, 0);
        return true;
    }

    public override int GetSize()
    {
        return GetStaticSize();
    }

    public static new int GetStaticSize()
    {
        // 4 bytes, one each for MovementPerTurn, RemainingMovement, VisitingRange, ScoutingRange,
        return StarvableUnitData.GetStaticSize() + 4 * sizeof(byte) + Location.GetStaticSize();
    }

    public override byte[] GetData()
    {
        NativeArray<byte> Bytes = SaveGameManager.GetArrayWithBaseFilled(this, base.GetSize(), base.GetData());

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
    
    public int MovementPerTurn = 1;
    [HideInInspector]
    public int RemainingMovement = 0;
    [HideInInspector]
    public Location Location;

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
