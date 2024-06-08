using System;
using Unity.Collections;
using static Stockpile;

public class Statistics : GameService, ISaveable
{
    private int BuildingsBuilt = 0;
    private int HexesMoved = 0;
    private int UnitsCreated = 0;
    private int ResourcesCollected = 0;

    private int BuildingsNeeded = 3;
    private int MovesNeeded = 10;
    private int UnitsNeeded = 2;
    private int ResourcesNeeded = 20;

    private int BuildingsIncrease = 2;
    private int MovesIncrease = 5;
    private int UnitsIncrease = 2;
    private int ResourcesIncrease = 10;


    private void CheckForPoints(ref int Collected, ref int Needed, int Increase, string Action, string Type)
    {
        // check this first, otherwise do not actually raise goal
        if (!Game.TryGetService(out Stockpile Stockpile))
            return;

        string Goal = Collected + "";
        int EarnedPoints = GetEarnedPoints(ref Collected, ref Needed, Increase);
        if (EarnedPoints == 0)
            return;

        Stockpile.UpgradePoints += EarnedPoints;

        string Upgrade = EarnedPoints > 1 ? "Upgrades" : "Upgrade";
        MessageSystem.CreateMessage(Message.Type.Success, "You earned " + EarnedPoints + " " + Upgrade + " for " + Action + " " + Goal + " " + Type);
    }

    private void CountResources(Production Production)
    {
        foreach (var Tuple in Production.GetTuples())
        {
            ResourcesCollected += Tuple.Value;
        }

        string Action = "collecting";
        string Type = "resources";
        CheckForPoints(ref ResourcesCollected, ref ResourcesNeeded, ResourcesIncrease, Action, Type);
    }

    private void CountUnit(UnitData UnitData)
    {
        UnitsCreated++; 
        
        string Action = "creating";
        string Type = "units";
        CheckForPoints(ref UnitsCreated, ref UnitsNeeded, UnitsIncrease, Action, Type);
    }

    private void CountMoves(int Moves)
    {
        HexesMoved += Moves;

        string Action = "moving";
        string Type = "";
        CheckForPoints(ref HexesMoved, ref MovesNeeded, MovesIncrease, Action, Type);
    }

    private void CountBuilding(BuildingData BuildingData)
    {
        BuildingsBuilt++;

        string Action = "creating";
        string Type = "buildings";
        CheckForPoints(ref BuildingsBuilt, ref BuildingsNeeded, BuildingsIncrease, Action, Type);
    }

    private int GetEarnedPoints(ref int Earned, ref int Goal, int GoalIncrease)
    {
        int Points = 0;
        while (Earned >= Goal)
        {
            Points++;
            Earned -= Goal;
            Goal += GoalIncrease;
        }
        return Points;
    }



    public byte[] GetData()
    {
        NativeArray<byte> Bytes = new(GetSize(), Allocator.Temp);
        int Pos = 0;

        Pos = SaveGameManager.AddInt(Bytes, Pos, BuildingsBuilt);
        Pos = SaveGameManager.AddInt(Bytes, Pos, HexesMoved);
        Pos = SaveGameManager.AddInt(Bytes, Pos, UnitsCreated);
        Pos = SaveGameManager.AddInt(Bytes, Pos, ResourcesCollected);

        Pos = SaveGameManager.AddInt(Bytes, Pos, BuildingsNeeded);
        Pos = SaveGameManager.AddInt(Bytes, Pos, MovesNeeded);
        Pos = SaveGameManager.AddInt(Bytes, Pos, UnitsNeeded);
        Pos = SaveGameManager.AddInt(Bytes, Pos, ResourcesNeeded);

        Pos = SaveGameManager.AddInt(Bytes, Pos, BuildingsIncrease);
        Pos = SaveGameManager.AddInt(Bytes, Pos, MovesIncrease);
        Pos = SaveGameManager.AddInt(Bytes, Pos, UnitsIncrease);
        Pos = SaveGameManager.AddInt(Bytes, Pos, ResourcesIncrease);

        return Bytes.ToArray();
    }

    public int GetSize()
    {
        return GetStaticSize();
    }

    public int GetStaticSize()
    {
        return sizeof(int) * 12;
    }

    public void SetData(NativeArray<byte> Bytes)
    {
        int Pos = 0;
        Pos = SaveGameManager.GetInt(Bytes, Pos, out BuildingsBuilt);
        Pos = SaveGameManager.GetInt(Bytes, Pos, out HexesMoved);
        Pos = SaveGameManager.GetInt(Bytes, Pos, out UnitsCreated);
        Pos = SaveGameManager.GetInt(Bytes, Pos, out ResourcesCollected);

        Pos = SaveGameManager.GetInt(Bytes, Pos, out BuildingsNeeded);
        Pos = SaveGameManager.GetInt(Bytes, Pos, out MovesNeeded);
        Pos = SaveGameManager.GetInt(Bytes, Pos, out UnitsNeeded);
        Pos = SaveGameManager.GetInt(Bytes, Pos, out ResourcesNeeded);

        Pos = SaveGameManager.GetInt(Bytes, Pos, out BuildingsIncrease);
        Pos = SaveGameManager.GetInt(Bytes, Pos, out MovesIncrease);
        Pos = SaveGameManager.GetInt(Bytes, Pos, out UnitsIncrease);
        Pos = SaveGameManager.GetInt(Bytes, Pos, out ResourcesIncrease);
    }

    protected override void StartServiceInternal()
    {
        Stockpile._OnResourcesCollected += CountResources;
        UnitData._OnUnitCreated += CountUnit;
        TokenizedUnitData._OnMovement += CountMoves;
        BuildingData._OnBuildingBuilt += CountBuilding;
    }

    protected override void StopServiceInternal() {}

    private void OnDestroy()
    {
        Stockpile._OnResourcesCollected -= CountResources;
        UnitData._OnUnitCreated -= CountUnit;
        TokenizedUnitData._OnMovement -= CountMoves;
        BuildingData._OnBuildingBuilt -= CountBuilding;
    }
}