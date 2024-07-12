using System;
using Unity.Collections;

/** 
 * Tracks how many things were created / moved etc during each playphase as well as overall
 * Also creates quests for those things tracked
 */
public class Statistics : GameService, ISaveableService
{
    // how many are counting towards the next upgrade
    // gets reset after each upgrade point
    private int BuildingsBuilt = 0;
    private int MovesDone = 0;
    private int UnitsCreated = 0;
    private int ResourcesCollected = 0;

    private int BuildingsNeeded = 0;
    private int MovesNeeded = 0;
    private int UnitsNeeded = 0;
    private int ResourcesNeeded = 0;

    private int BuildingsIncrease = 0;
    private int MovesIncrease = 0;
    private int UnitsIncrease = 0;
    private int ResourcesIncrease = 0;

    public int BestHighscore = 0;
    public int BestBuildings = 0;
    public int BestMoves = 0;
    public int BestUnits = 0;
    public int BestResources = 0;

    public int CurrentBuildings = 0;
    public int CurrentMoves = 0;
    public int CurrentUnits = 0;
    public int CurrentResources = 0;

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

    private int CountResources(Production Production)
    {
        int Collected = 0;
        foreach (var Tuple in Production.GetTuples())
        {
            Collected += Tuple.Value;
            ResourcesCollected += Tuple.Value;
        }
        CurrentResources += Collected;
        BestResources = Math.Max(CurrentResources, BestResources);

        return Collected;
    }

    private int CountUnit(UnitData UnitData)
    {
        UnitsCreated++;
        CurrentUnits++;
        BestUnits = Math.Max(CurrentUnits, BestUnits);

        return 1;
    }

    private int CountMoves(HexagonData.DiscoveryState NewState)
    {
        if (NewState < HexagonData.DiscoveryState.Visited)
            return 0;

        MovesDone += 1;
        CurrentMoves += 1;
        BestMoves = Math.Max(CurrentMoves, BestMoves);
        return 1;
    }

    private int CountBuilding(BuildingData BuildingData)
    {
        BuildingsBuilt++;
        CurrentBuildings++;
        BestBuildings = Math.Max(CurrentBuildings, BestBuildings);
        return 1;
    }

    private void IncreaseTarget(ref int Earned, ref int Goal, int GoalIncrease)
    {
        while (Earned >= Goal)
        {
            Earned -= Goal;
            Goal += GoalIncrease;
        }
    }

    private void IncreaseUnits()
    {
        IncreaseTarget(ref UnitsCreated, ref UnitsNeeded, UnitsIncrease);
    }

    private void IncreaseResources()
    {
        IncreaseTarget(ref ResourcesCollected, ref ResourcesNeeded, ResourcesIncrease);
    }

    private void IncreaseMoves()
    {
        IncreaseTarget(ref MovesDone, ref MovesNeeded, MovesIncrease);
    }

    private void IncreaseBuildings()
    {
        IncreaseTarget(ref BuildingsBuilt, ref BuildingsNeeded, BuildingsIncrease);
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

    public void CreateQuests()
    {
        if (!Game.TryGetServices(out QuestService QuestService, out IconFactory IconFactory))
            return;

        QuestService.AddQuest<UnitData>(
            UnitData.RegisterQuest,
            UnitData.DeregisterQuest,
            UnitsCreated,
            UnitsNeeded,
            IconFactory.GetIconForMisc(IconFactory.MiscellaneousType.Worker),
            CountUnit,
            IncreaseUnits,
            "Create additional units",
            Quest.Type.Positive
        );

        QuestService.AddQuest<Production>(
            Stockpile.RegisterQuest,
            Stockpile.DeregisterQuest,
            ResourcesCollected,
            ResourcesNeeded,
            IconFactory.GetIconForProductionType(Production.GoodsType.BuildingMaterials),
            CountResources,
            IncreaseResources,
            "Collect additional resources",
            Quest.Type.Positive
        );

        QuestService.AddQuest<HexagonData.DiscoveryState>(
            HexagonData.RegisterQuest,
            HexagonData.DeregisterQuest,
            MovesDone,
            MovesNeeded,
            IconFactory.GetIconForMisc(IconFactory.MiscellaneousType.Scoutings),
            CountMoves,
            IncreaseMoves,
            "Reveal additional tiles",
            Quest.Type.Positive
        );

        QuestService.AddQuest<BuildingData>(
            BuildingData.RegisterQuest, 
            BuildingData.DeregisterQuest,
            BuildingsBuilt,
            BuildingsNeeded,
            IconFactory.GetIconForMisc(IconFactory.MiscellaneousType.Buildings),
            CountBuilding,
            IncreaseBuildings,
            "Build additional buildings",
            Quest.Type.Positive
        );
    }

    protected override void StartServiceInternal()
    {
        SaveGameManager.RunIfNotInSavegame(() =>
        {
            ResetAllStats();

            Game.RunAfterServicesInit((QuestService QuestService, MapGenerator MapGenerator) =>
            {
                CreateQuests();
            });

            _OnInit?.Invoke(this);
        }, ISaveableService.SaveGameType.Statistics);
    }

    public void Load()
    {
        Game.RunAfterServiceInit((QuestService QuestService) =>
        {
            CreateQuests();
        });
    }

    public void Reset()
    {
        ResetAllStats();
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

    protected override void StopServiceInternal() {}

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
