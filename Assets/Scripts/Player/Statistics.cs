using System;
using Unity.Collections;
using UnityEngine;
using static HexagonData;

/** 
 * Tracks how many things were created / moved etc during each playphase as well as overall
 * Also creates quests for those things tracked
 */
public class Statistics : GameService, ISaveableService
{
    // how many are counting towards the next upgrade
    // gets reset after each upgrade point
    [HideInInspector]public int BuildingsBuilt = 0;
    [HideInInspector]public int MovesDone = 0;
    [HideInInspector]public int UnitsCreated = 0;
    [HideInInspector]public int ResourcesCollected = 0;

    // used by their respective quests as target values
    [HideInInspector]public int BuildingsNeeded = 0;
    [HideInInspector]public int MovesNeeded = 0;
    [HideInInspector]public int UnitsNeeded = 0;
    [HideInInspector]public int ResourcesNeeded = 0;

    [HideInInspector]public int BuildingsIncrease = 0;
    [HideInInspector]public int MovesIncrease = 0;
    [HideInInspector]public int UnitsIncrease = 0;
    [HideInInspector]public int ResourcesIncrease = 0;

    [HideInInspector]public int BestHighscore = 0;
    [HideInInspector]public int BestBuildings = 0;
    [HideInInspector]public int BestMoves = 0;
    [HideInInspector]public int BestUnits = 0;
    [HideInInspector]public int BestResources = 0;

    [HideInInspector]public int CurrentBuildings = 0;
    [HideInInspector]public int CurrentMoves = 0;
    [HideInInspector]public int CurrentUnits = 0;
    [HideInInspector]public int CurrentResources = 0;

    public int GetHighscore()
    {
        int Highscore =
            CurrentBuildings * POINTS_BUILDINGS +
            CurrentMoves * POINTS_HEXES +
            CurrentUnits * POINTS_UNITS +
            CurrentResources * POINTS_RESOURCES;

        if (Highscore > BestHighscore)
        {
            BestHighscore = Highscore;
        }
        return Highscore;
    }

    public void IncreaseTarget(ref int Goal, int GoalIncrease)
    {
        Goal += GoalIncrease;
    }

    public void CountBuilding(BuildingEntity Building)
    {
        BuildingsBuilt++;
        CurrentBuildings++;
        BestBuildings = Mathf.Max(BestBuildings, CurrentBuildings);
    }

    public void CountUnit(ScriptableEntity Unit)
    {
        if (Unit is not UnitEntity)
            return;

        UnitsCreated++;
        CurrentUnits++;
        BestUnits = Mathf.Max(BestUnits, CurrentUnits);
    }

    public void CountResources(Production Production)
    {
        int Collected = 0;
        foreach (var Tuple in Production.GetTuples())
        {
            Collected += Mathf.Max(Tuple.Value, 0);
            ResourcesCollected += Tuple.Value;
        }
        CurrentResources += Collected;
        BestResources = Math.Max(CurrentResources, BestResources);
    }

    public void CountMoves(DiscoveryState State)
    {
        if (State < DiscoveryState.Visited)
            return;

        MovesDone += 1;
        CurrentMoves += 1;
        BestMoves = Math.Max(CurrentMoves, BestMoves);
    }

    public byte[] GetData()
    {
        NativeArray<byte> Bytes = new(GetSize(), Allocator.Temp);
        int Pos = 0;

        Pos = SaveGameManager.AddInt(Bytes, Pos, BuildingsBuilt);
        Pos = SaveGameManager.AddInt(Bytes, Pos, MovesDone);
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

        Pos = SaveGameManager.AddInt(Bytes, Pos, BestHighscore);

        Pos = SaveGameManager.AddInt(Bytes, Pos, BestBuildings);
        Pos = SaveGameManager.AddInt(Bytes, Pos, BestMoves);
        Pos = SaveGameManager.AddInt(Bytes, Pos, BestUnits);
        Pos = SaveGameManager.AddInt(Bytes, Pos, BestResources);

        Pos = SaveGameManager.AddInt(Bytes, Pos, CurrentBuildings);
        Pos = SaveGameManager.AddInt(Bytes, Pos, CurrentMoves);
        Pos = SaveGameManager.AddInt(Bytes, Pos, CurrentUnits);
        Pos = SaveGameManager.AddInt(Bytes, Pos, CurrentResources);

        return Bytes.ToArray();
    }

    public int GetSize()
    {
        return GetStaticSize();
    }

    public int GetStaticSize()
    {
        return sizeof(int) * 21;
    }

    private void Subscribe(bool bSubscribe)
    {
        if (bSubscribe)
        {
            Units._OnEntityCreated.Add(CountUnit);
            Workers._OnEntityCreated.Add(CountUnit);
            MapGenerator._OnDiscoveredTile.Add(CountMoves);
            Stockpile._OnResourcesCollected.Add(CountResources);
            BuildingService._OnBuildingBuilt.Add(CountBuilding);
        }
        else
        {
            Units._OnEntityCreated.Remove(CountUnit);
            Workers._OnEntityCreated.Remove(CountUnit);
            MapGenerator._OnDiscoveredTile.Remove(CountMoves);
            Stockpile._OnResourcesCollected.Remove(CountResources);
            BuildingService._OnBuildingBuilt.Remove(CountBuilding);
        }
    }

    public void SetData(NativeArray<byte> Bytes)
    {
        int Pos = 0;
        Pos = SaveGameManager.GetInt(Bytes, Pos, out BuildingsBuilt);
        Pos = SaveGameManager.GetInt(Bytes, Pos, out MovesDone);
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

        Pos = SaveGameManager.GetInt(Bytes, Pos, out BestHighscore);

        Pos = SaveGameManager.GetInt(Bytes, Pos, out BestBuildings);
        Pos = SaveGameManager.GetInt(Bytes, Pos, out BestMoves);
        Pos = SaveGameManager.GetInt(Bytes, Pos, out BestUnits);
        Pos = SaveGameManager.GetInt(Bytes, Pos, out BestResources);

        Pos = SaveGameManager.GetInt(Bytes, Pos, out CurrentBuildings);
        Pos = SaveGameManager.GetInt(Bytes, Pos, out CurrentMoves);
        Pos = SaveGameManager.GetInt(Bytes, Pos, out CurrentUnits);
        Pos = SaveGameManager.GetInt(Bytes, Pos, out CurrentResources);
    }

    protected override void StartServiceInternal()
    {
        Game.RunAfterServiceInit((SaveGameManager Manager) =>
        {
            Subscribe(true);
            if (!Manager.HasDataFor(ISaveableService.SaveGameType.Statistics))
            {
                ResetAllStats();
            }
            _OnInit?.Invoke(this);
        });
    }

    public void Reset()
    {
        ResetAllStats();
        Subscribe(false);
    }

    private void ResetAllStats()
    {
        BuildingsBuilt = 0;
        MovesDone = 0;
        UnitsCreated = 0;
        ResourcesCollected = 0;

        BuildingsNeeded = BASE_BUILDINGSNEEDED;
        MovesNeeded = BASE_MOVESNEEDED;
        UnitsNeeded = BASE_UNITSNEEDED;
        ResourcesNeeded = BASE_RESOURCESNEEDED;

        BuildingsIncrease = BASE_BUILDINGSINCREASE;
        MovesIncrease = BASE_MOVESINCREASE;
        UnitsIncrease = BASE_UNITSINCREASE;
        ResourcesIncrease = BASE_RESOURCESINCREASE;

        BestHighscore = 0;
        BestBuildings = 0;
        BestMoves = 0;
        BestUnits = 0;
        BestResources = 0;

        CurrentBuildings = 0;
        CurrentMoves = 0;
        CurrentUnits = 0;
        CurrentResources = 0;
    }

    public void ResetCurrentStats()
    {
        CurrentBuildings = 0;
        CurrentMoves = 0;
        CurrentResources = 0;
        CurrentUnits = 0;
    }

    protected override void StopServiceInternal() {
        Subscribe(false);
    }

    private static int POINTS_BUILDINGS = 5;
    private static int POINTS_UNITS = 3;
    private static int POINTS_HEXES = 2;
    private static int POINTS_RESOURCES = 1;

    private static int BASE_BUILDINGSNEEDED = 3;
    private static int BASE_MOVESNEEDED = 10;
    private static int BASE_UNITSNEEDED = 2;
    private static int BASE_RESOURCESNEEDED = 20;
    private static int BASE_BUILDINGSINCREASE = 2;
    private static int BASE_MOVESINCREASE = 5;
    private static int BASE_UNITSINCREASE = 2;
    private static int BASE_RESOURCESINCREASE = 10;
}
