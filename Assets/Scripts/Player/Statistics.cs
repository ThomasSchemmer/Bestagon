using System;
using Unity.Collections;
using UnityEngine;
using static HexagonData;

/** 
 * Tracks how many things were created / moved etc during each playphase as well as overall
 * Also creates quests for those things tracked
 */
public class Statistics : SaveableService
{
    // how many are counting towards the next upgrade
    // gets reset after each upgrade point
    [HideInInspector][SaveableBaseType] public int BuildingsBuilt = 0;
    [HideInInspector][SaveableBaseType] public int MovesDone = 0;
    [HideInInspector][SaveableBaseType] public int UnitsCreated = 0;
    [HideInInspector][SaveableBaseType] public int ResourcesCollected = 0;

    // used by their respective quests as target values
    [HideInInspector][SaveableBaseType] public int BuildingsNeeded = 0;
    [HideInInspector][SaveableBaseType] public int MovesNeeded = 0;
    [HideInInspector][SaveableBaseType] public int UnitsNeeded = 0;
    [HideInInspector][SaveableBaseType] public int ResourcesNeeded = 0;

    [HideInInspector][SaveableBaseType] public int BuildingsIncrease = 0;
    [HideInInspector][SaveableBaseType] public int MovesIncrease = 0;
    [HideInInspector][SaveableBaseType] public int UnitsIncrease = 0;
    [HideInInspector][SaveableBaseType] public int ResourcesIncrease = 0;

    [HideInInspector][SaveableBaseType] public int BestHighscore = 0;
    [HideInInspector][SaveableBaseType] public int BestBuildings = 0;
    [HideInInspector][SaveableBaseType] public int BestMoves = 0;
    [HideInInspector][SaveableBaseType] public int BestUnits = 0;
    [HideInInspector][SaveableBaseType] public int BestResources = 0;

    [HideInInspector][SaveableBaseType] public int CurrentBuildings = 0;
    [HideInInspector][SaveableBaseType] public int CurrentMoves = 0;
    [HideInInspector][SaveableBaseType] public int CurrentUnits = 0;
    [HideInInspector][SaveableBaseType] public int CurrentResources = 0;

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
    protected override void StartServiceInternal()
    {
        Game.RunAfterServiceInit((SaveGameManager Manager) =>
        {
            Subscribe(true);
            if (!Manager.HasDataFor(SaveableService.SaveGameType.Statistics))
            {
                ResetAllStats();
            }
            _OnInit?.Invoke(this);
        });
    }

    public override void Reset()
    {
        base.Reset();
        ResetAllStats();
        Subscribe(false);
    }

    public override void OnAfterLoaded()
    {
        base.OnAfterLoaded();
        Subscribe(true);
        _OnInit?.Invoke(this);
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

    private void ResetCurrentStats()
    {
        CurrentBuildings = 0;
        CurrentMoves = 0;
        CurrentResources = 0;
        CurrentUnits = 0;
    }

    protected override void StopServiceInternal() {
        Subscribe(false);
    }

    public override void OnBeforeSaved(bool bShouldReset)
    {
        ResetCurrentStats();
    }

    public GameObject GetGameObject() { return gameObject; }

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
