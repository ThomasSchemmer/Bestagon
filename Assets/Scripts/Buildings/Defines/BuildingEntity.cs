using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using static CardUpgradeScreen;

[CreateAssetMenu(fileName = "Building", menuName = "ScriptableObjects/Building", order = 1)]
public class BuildingEntity : ScriptableEntity, IPreviewable, ITokenized
{
    [HideInInspector]
    public WorkerEntity[] AssignedWorkers;
    public BuildingConfig.Type BuildingType = BuildingConfig.Type.DEFAULT;
    public Production Cost = new Production();
    public OnTurnBuildingEffect Effect = null;
    public int MaxWorker = 1;
    public HexagonConfig.HexagonType BuildableOn = 0;
    [HideInInspector]
    public int CurrentUsages = 1;
    public int MaxUsages = 1;

    public int UpgradeMaxWorker = 1;
    public HexagonConfig.HexagonType UpgradeBuildableOn = 0;
    public int UpgradeMaxUsages = 1;

    public LocationSet.AreaSize Area = LocationSet.AreaSize.Single;

    // can be a multi-tile location
    protected LocationSet Locations;
    protected int Angle;

    public BuildingEntity() {
        // used on creating ScriptableObjects, don't delete!
        EntityType = EType.Building;
        Cost = new();
        Effect = new();
        Init();
    }

    public void Init()
    {
        Locations = new(Location.Invalid);
        AssignedWorkers = new WorkerEntity[MaxWorker];
        Effect.Init(this);
    }

    public virtual Vector3 GetOffset() {
        return new Vector3(0, 5, 0);
    }

    public virtual Quaternion GetRotation() {
        return Quaternion.Euler(0, 180 + Angle * 60, 0);
    }

    public bool IsPreviewInteractableWith(HexagonVisualization Hex, bool bIsPreview)
    {
        // ignore the preview parameter, as the error messages are only created in the BuildingCard
        return CanBeBuildOn(Hex, true, out string _);
    }

    public Production GetCosts()
    {
        float Multiplier = AttributeSet.Get()[AttributeType.BuildingCostRate].CurrentValue;
        int Count = 0;
        // maybe remove empty
        List<Tuple<Production.Type, int>> Tuples = Cost.GetTuples();
        foreach (var Tuple in Tuples)
        {
            Count += Tuple.Value;
        }
        int ActualCount = Mathf.RoundToInt(Count * Multiplier);
        int Difference = ActualCount - Count;
        int Sign = (int)Mathf.Sign(Difference);

        UnityEngine.Random.InitState(BuildingType.GetHashCode());
        for (int i = 0; i < Mathf.Abs(Difference); i++)
        {
            int ResourceIndex = UnityEngine.Random.Range(0, Tuples.Count);
            var OldTuple = Tuples[ResourceIndex];
            
            Tuple<Production.Type, int> NewTuple = new (OldTuple.Key, OldTuple.Value + 1 * Sign);
            Tuples[ResourceIndex] = NewTuple;
        }

       return new(Tuples);
    }

    public Production GetProduction(bool bIsSimulated)
    {
        return Effect.GetProduction(GetWorkerMultiplier(bIsSimulated), Locations, bIsSimulated);
    }

    public void SimulateCurrentFood()
    {
        foreach (var Worker in AssignedWorkers)
        {
            if (Worker == null)
                continue;

            Worker.SimulatedFoodCount = Worker.CurrentFoodCount;
        }
    }

    public Production GetTheoreticalMaximumProduction()
    {
        return Effect.GetProduction(AssignedWorkers.Length, Locations, false);
    }

    public Production GetProductionPreview(LocationSet Locations)
    {
        return Effect.GetProduction(GetMaximumWorkerCount(), Locations, false);
    }

    public bool TryGetAdjacencyBonus(out Dictionary<HexagonConfig.HexagonType, Production> Bonus)
    {
        return Effect.TryGetAdjacencyBonus(out Bonus);
    }

    public bool CanBeBuildOn(HexagonVisualization Hex, bool bShouldCheckCosts, out string Reason) {
        if (!LocationSet.TryGetAround(Hex.Location, Area, out LocationSet NewLocations))
        {
            Reason = "not enough space";
            return false;
        }

        foreach (Location Location in NewLocations)
        {
            if (!CanBeBuildOn(Location, bShouldCheckCosts, out Reason))
                return false;
        }

        if (!Game.TryGetServices(out Stockpile Stockpile, out DecorationService Decorations))
        {
            Reason = "Invalid";
            return false;
        }

        if (Decorations.IsEntityAt(NewLocations))
        {
            Reason = "blocked by decoration";
            return false;
        }

        if (bShouldCheckCosts && !Stockpile.CanAfford(GetCosts()))
        {
            Reason = "insufficient resources";
            return false;
        }

        Reason = string.Empty;
        return true;
    }

    private bool CanBeBuildOn(Location Location, bool bShouldCheckCosts, out string Reason)
    {
        Reason = "Invalid";
        if (!Game.TryGetService(out MapGenerator MapGenerator))
            return false;

        if (!MapGenerator.TryGetHexagonData(Location, out var Hex))
        {
            Reason = "Invalid Hex";
            return false;
        }

        if (!BuildableOn.HasFlag(Hex.Type))
        {
            Reason = "not buildable on " + Hex.Type;
            return false;
        }

        if (Hex.GetDiscoveryState() != HexagonData.DiscoveryState.Visited)
        {
            Reason = "only buildable on scouted hexes";
            return false;
        }

        if (!Game.TryGetService(out ReachVisualization ReachVisualization))
        {
            Reason = "Invalid Hex";
            return false;
        }

        if (!ReachVisualization.CheckFor(Hex.Location))
        {
            Reason = "must be in reach of other buildings";
            return false;
        }

        if (!Game.TryGetService(out BuildingService Buildings))
        {
            Reason = "Invalid Hex";
            return false;
        }

        if (Buildings.IsEntityAt(Hex.Location))
        {
            Reason = "another building exists already";
            return false;
        }

        if (Hex.IsMalaised())
        {
            Reason = "hex is malaised";
            return false;
        }
        return true;
    }

    public void BuildAt(LocationSet Location, int Angle)
    {
        if (!Game.TryGetService(out MapGenerator Generator))
            return;

        this.Locations = Location.Copy();
        this.Angle = Angle;
        Generator.AddBuilding(this);
    }

    public int GetMaximumWorkerCount() {
        return AssignedWorkers.Length;
    }

    public int GetWorkingWorkerCount(bool bIsSimulated)
    {
        int WorkerCount = 0;
        for (int i = 0; i < AssignedWorkers.Length; i++)
        {
            WorkerCount += AssignedWorkers[i] != null && AssignedWorkers[i].IsReadyToWork(bIsSimulated) ? 1 : 0;
        }
        return WorkerCount;
    }

    public int GetAssignedWorkerCount()
    {
        int WorkerCount = 0;
        for (int i = 0; i < AssignedWorkers.Length; i++)
        {
            WorkerCount += AssignedWorkers[i] != null ? 1 : 0;
        }
        return WorkerCount;
    }

    public int GetWorkerMultiplier(bool bIsSimulated) {
        return GetWorkingWorkerCount(bIsSimulated);
    }

    public void RequestAddWorkerAt(int i) {
        if (AssignedWorkers[i] != null)
            return;

        if (!Game.TryGetService(out Workers Workers))
            return;

        Workers.RequestAddWorkerFor(this, i);
    }

    public void RequestRemoveWorkerAt(int i)
    {
        if (AssignedWorkers[i] == null)
            return;

        if (!Game.TryGetService(out Workers Workers))
            return;

        Workers.RequestRemoveWorkerFor(this, AssignedWorkers[i], i);
    }

    public void PutWorkerAt(WorkerEntity Worker, int i)
    {
        if (AssignedWorkers[i] != null)
            return;

        AssignedWorkers[i] = Worker;
    }

    public void RemoveWorker(int i)
    {
        AssignedWorkers[i] = null;
    }
    public override bool Equals(object Other) {
        if (Other is not BuildingEntity) 
            return false;

        BuildingEntity OtherBuilding = (BuildingEntity)Other;
        return Locations.Equals(OtherBuilding.Locations);
    }

    public override int GetHashCode() {
        return Locations.GetHashCode() + "Building".GetHashCode();
    }

    public bool IsFoodProductionBuilding()
    {
        int FoodValue = 0;
        foreach (var Tuple in GetTheoreticalMaximumProduction().GetTuples())
        {
            FoodValue += Production.GetNutrition(Tuple.Key) * Tuple.Value;
        }
        return FoodValue > 0;
    }

    public void Upgrade(UpgradeableAttributes SelectedAttribute)
    {
        if (!IsUpgradePossible(SelectedAttribute))
            return;

        switch (SelectedAttribute)
        {
            case UpgradeableAttributes.MaxUsages:
                MaxUsages = Mathf.Clamp(MaxUsages + 1, 0, UpgradeMaxUsages);
                CurrentUsages = MaxUsages;
                return;
            case UpgradeableAttributes.MaxWorker:
                MaxWorker = Mathf.Clamp(MaxWorker + 1, 0, UpgradeMaxWorker); 
                return;
            case UpgradeableAttributes.Production:
                UpgradeProduction();
                return;
            case UpgradeableAttributes.BuildableTiles:
                UpgradeBuildableTiles();
                return;
        }
    }

    private void UpgradeProduction()
    {
        Production Difference = Effect.UpgradeProduction - Effect.Production;
        foreach (var Tuple in Difference.GetTuples())
        {
            if (Tuple.Value == 0)
                continue;

            Effect.Production[Tuple.Key]++;
            return;
        }
    }

    private void UpgradeBuildableTiles()
    {
        if (!Game.TryGetService(out MapGenerator MapGenerator))
            return;

        HexagonConfig.HexagonType[] GroupMasks = MapGenerator.UnlockableTypes.GetCategoryMasks();

        // theoretically inefficient, as we have to loop for each of the groups
        // but since we rarely call it its okay
        foreach (var GroupMask in GroupMasks)
        {
            for (int i = 0; i <= HexagonConfig.MaxTypeIndex; i++)
            {
                // already unlocked
                if (((int)BuildableOn & (1 << i)) > 0)
                    continue;

                // not available in this group
                if (((int)GroupMask & (1 << i)) == 0)
                    continue;

                // not available to upgrade
                if (((int)UpgradeBuildableOn & (1 << i)) == 0)
                    continue;

                BuildableOn |= (HexagonConfig.HexagonType)(1 << i);
                return;
            }
        }
    }

    public bool IsAnyUpgradePossible()
    {
        // TODO: remove once fixed
        if (BuildingType == BuildingConfig.Type.Hut)
            return false;

        return IsUpgradePossible(UpgradeableAttributes.MaxUsages) ||
            IsUpgradePossible(UpgradeableAttributes.MaxWorker) ||
            IsUpgradePossible(UpgradeableAttributes.Production);
    }

    public bool IsUpgradePossible(UpgradeableAttributes SelectedAttribute)
    {
        // dont forget to update IsAnyUpgradePossible!
        switch (SelectedAttribute)
        {
            case UpgradeableAttributes.MaxUsages:
                return MaxUsages < UpgradeMaxUsages;
            case UpgradeableAttributes.MaxWorker:
                return MaxWorker < UpgradeMaxWorker;
            case UpgradeableAttributes.BuildableTiles:
                return BuildableOn != UpgradeBuildableOn;
            case UpgradeableAttributes.Production:
                return Effect.Production.SmallerThanAny(Effect.UpgradeProduction);
        }
        return false;
    }

    public void SetLocation(LocationSet Location)
    {
        this.Locations = Location;
    }

    public LocationSet GetLocations() { 
        return this.Locations; 
    }

    public void SetAngle(int Angle)
    {
        this.Angle = Angle;
    }

    public void RefreshUsage()
    {
        CurrentUsages = MaxUsages;
    }

    public override bool IsAboutToBeMalaised()
    {
        if (!Game.TryGetService(out MapGenerator MapGenerator))
            return false;

        if (!MapGenerator.TryGetHexagonData(Locations, out List<HexagonData> Hexes))
            return false;

        return Hexes.Any(Hex => Hex.IsPreMalaised());
    }

    public Location GetLocationAboutToBeMalaised()
    {
        if (!Game.TryGetService(out MapGenerator MapGenerator))
            return Location.Invalid;

        if (!MapGenerator.TryGetHexagonData(Locations, out List<HexagonData> Hexes))
            return Location.Invalid;

        List<HexagonData> PreMalaisedHexes = Hexes.Where(Hex => Hex.IsPreMalaised()).ToList();
        return PreMalaisedHexes.Count > 0 ? PreMalaisedHexes[0].Location : Location.Invalid;
    }

    public override int GetSize()
    {
        return GetStaticSize();
    }

    public static new int GetStaticSize()
    {
        // BuildingType Type and buildable on, max workers
        // Workers themselfs will be assigned later
        return ScriptableEntity.GetStaticSize() +
            LocationSet.GetStaticSize(LocationSet.MaxCount) +
            Production.GetStaticSize() +
            OnTurnBuildingEffect.GetStaticSize() +
            sizeof(byte) * 2 +
            sizeof(int) * 8;
    }

    public override byte[] GetData()
    {
        NativeArray<byte> Bytes = SaveGameManager.GetArrayWithBaseFilled(BuildingEntity.GetStaticSize(), base.GetSize(), base.GetData());

        int Pos = base.GetSize();
        byte TypeAsByte = (byte)HexagonConfig.MaskToInt((int)BuildingType, 32);
        Pos = SaveGameManager.AddEnumAsByte(Bytes, Pos, TypeAsByte);
        Pos = SaveGameManager.AddSaveable(Bytes, Pos, Locations);
        Pos = SaveGameManager.AddSaveable(Bytes, Pos, Cost);
        Pos = SaveGameManager.AddInt(Bytes, Pos, (int)BuildableOn);
        Pos = SaveGameManager.AddSaveable(Bytes, Pos, Effect);
        Pos = SaveGameManager.AddInt(Bytes, Pos, MaxWorker);
        Pos = SaveGameManager.AddInt(Bytes, Pos, CurrentUsages);
        Pos = SaveGameManager.AddInt(Bytes, Pos, MaxUsages);
        Pos = SaveGameManager.AddInt(Bytes, Pos, UpgradeMaxWorker);
        Pos = SaveGameManager.AddInt(Bytes, Pos, (int)UpgradeBuildableOn);
        Pos = SaveGameManager.AddInt(Bytes, Pos, UpgradeMaxUsages);
        Pos = SaveGameManager.AddInt(Bytes, Pos, Angle);
        Pos = SaveGameManager.AddEnumAsByte(Bytes, Pos, (byte)Area);


        return Bytes.ToArray();
    }

    public override void SetData(NativeArray<byte> Bytes)
    {
        base.SetData(Bytes);
        int Pos = base.GetSize();

        Pos = SaveGameManager.GetEnumAsByte(Bytes, Pos, out byte bBuildingType);
        Pos = SaveGameManager.SetSaveable(Bytes, Pos, Locations);
        Pos = SaveGameManager.SetSaveable(Bytes, Pos, Cost);
        Pos = SaveGameManager.GetInt(Bytes, Pos, out int iBuildableOn);
        Pos = SaveGameManager.SetSaveable(Bytes, Pos, Effect);
        Pos = SaveGameManager.GetInt(Bytes, Pos, out MaxWorker);
        Pos = SaveGameManager.GetInt(Bytes, Pos, out CurrentUsages);
        Pos = SaveGameManager.GetInt(Bytes, Pos, out MaxUsages);
        Pos = SaveGameManager.GetInt(Bytes, Pos, out UpgradeMaxWorker);
        Pos = SaveGameManager.GetInt(Bytes, Pos, out int iUpgradeBuildableOn);
        Pos = SaveGameManager.GetInt(Bytes, Pos, out UpgradeMaxUsages);
        Pos = SaveGameManager.GetInt(Bytes, Pos, out Angle);
        Pos = SaveGameManager.GetEnumAsByte(Bytes, Pos, out byte bArea);

        BuildingType = (BuildingConfig.Type)HexagonConfig.IntToMask(bBuildingType);
        BuildableOn = (HexagonConfig.HexagonType)iBuildableOn;
        UpgradeBuildableOn = (HexagonConfig.HexagonType)iUpgradeBuildableOn;
        Area = (LocationSet.AreaSize)bArea;

        AssignedWorkers = new WorkerEntity[MaxWorker];
    }

    public new static int CreateFromSave(NativeArray<byte> Bytes, int Pos, out ScriptableEntity Building)
    {
        Building = default;
        if (!Game.TryGetService(out MeshFactory MeshFactory))
            return -1;

        Pos = SaveGameManager.GetEnumAsByte(Bytes, Pos, out byte _);
        Pos = SaveGameManager.GetEnumAsByte(Bytes, Pos, out byte bBuildingType);
        BuildingConfig.Type Type = (BuildingConfig.Type)HexagonConfig.IntToMask(bBuildingType);

        Building = MeshFactory.CreateDataFromType(Type);
        return Pos;
    }

}
