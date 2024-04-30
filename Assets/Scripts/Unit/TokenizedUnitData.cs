using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Assertions;

/** 
 * Any unit that is represented by an in-game token. Also supports moving this token
 * Still only contains data and is not directly linked to the token itself ("MonoBehaviour"), see 
 * @UnitVisualization for that. During lifetime will be managed by @Units and only exists there
 */
public abstract class TokenizedUnitData : StarvableUnitData
{
    public abstract string GetPrefabName();

    public int GetMovementCostTo(Location ToLocation)
    {
        List<Location> Path = Pathfinding.FindPathFromTo(Location, ToLocation);
        return Pathfinding.GetCostsForPath(Path);
    }

    public void MoveTo(Location Location, int Costs)
    {
        this.Location = Location;
        this.RemainingMovement -= Costs;

        if (!Game.TryGetService(out MapGenerator MapGenerator))
            return;

        if (!MapGenerator.TryGetHexagon(Location, out HexagonVisualization NewHex))
            return;

        NewHex.UpdateDiscoveryState(VisitingRange, ScoutingRange);
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

    public override int GetSize()
    {
        // 4 bytes, one each for MovementPerTurn, RemainingMovement, VisitingRange, ScoutingRange,
        return base.GetSize() + 4 * sizeof(byte) + Location.GetStaticSize();
    }

    public override byte[] GetData()
    {
        NativeArray<byte> Bytes = SaveGameManager.GetArrayWithBaseFilled(this, GetSize(), base.GetData());

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
    public int RemainingMovement = 0;
    public Location Location;

    // should always be greater than the MovementPerTurn!
    public int VisitingRange = 3;
    public int ScoutingRange = 4;

    // don't save, its only temporarily constructed anyway
    public UnitVisualization Visualization;
}
