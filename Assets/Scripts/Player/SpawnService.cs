using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public class SpawnService : GameService
{
    protected override void StartServiceInternal()
    {
        Game.RunAfterServicesInit((MapGenerator MapGenerator, CameraController Controller) =>
        {
            if (Game.Instance.State == Game.GameState.CardSelection)
                return;

            InitVisibility();
            TeleportToStart();
            HexagonData._OnHexDiscoveryState += OnHexDiscovered;
            HexagonData._OnHexMalaised += OnHexMalaised;
        });
    }

    public void InitVisibility()
    {
        if (!Game.TryGetService(out MapGenerator MapGenerator))
            return;

        Location TargetLocation = new Location((int)StartLocation.x, (int)StartLocation.y, (int)StartLocation.z, (int)StartLocation.w);
        if (!MapGenerator.TryGetHexagon(TargetLocation, out HexagonVisualization Hex))
            return;

        Hex.UpdateDiscoveryState(StartVisibilityRange, StartScoutingRange);
    }

    private void TeleportToStart()
    {

        if (!Game.TryGetService(out CameraController CameraController))
            return;

        Location TargetLocation = new Location((int)StartLocation.x, (int)StartLocation.y, (int)StartLocation.z, (int)StartLocation.w);

        CameraController.TeleportTo(TargetLocation.WorldLocation);
    }

    protected override void ResetInternal()
    {
        
        HexagonData._OnHexDiscoveryState -= OnHexDiscovered;
        HexagonData._OnHexMalaised -= OnHexMalaised;
    }

    private void OnDestroy()
    {
        ResetInternal();
    }

    private void OnHexDiscovered(HexagonData Data, HexagonData.DiscoveryState State)
    {
        if (Game.Instance.Mode == Game.GameMode.MapEditor)
            return;
        if (State != HexagonData.DiscoveryState.Visited)
            return;

        DiscoveredCount++;
        if (DiscoveredCount < CountToSpawnDecoration)
            return;

        if (!Game.TryGetService(out DecorationService DecorationService))
            return;

        if (!Data.CanDecorationSpawn() || DecorationService.IsEntityAt(Data.Location))
            return;

        DiscoveredCount -= CountToSpawnDecoration;
        CountToSpawnDecoration += CountToSpawnDecorationIncrease;
        CountToSpawnDecoration = Mathf.Min(CountToSpawnDecoration, MaxCountToSpawn);

        // this order is dependend on type spawn weight: high -> low!
        List<DecorationEntity.DType> SpawnableTypes = new();
        SpawnableTypes.Add(DecorationEntity.DType.Tribe);
        SpawnableTypes.Add(DecorationEntity.DType.Ruins);
        if (CanSpawnTreasure(Data.Type))
        {
            SpawnableTypes.Add(DecorationEntity.DType.Relic);
        }
        SpawnableTypes.Add(DecorationEntity.DType.Altar);

        DecorationEntity.DType Decoration = GetTypeToSpawn(Data, SpawnableTypes);
        DecorationService.CreateNewDecoration(Decoration, Data.Location);
    }

    private void OnHexMalaised(HexagonData Data)
    {
        if (!Game.TryGetServices(out AmberService Ambers, out DecorationService Decorations))
            return;

        if (!Ambers.IsUnlocked())
            return;

        if (!Ambers.ShouldSpawnMoreAmber())
            return;

        if (!Data.CanDecorationSpawn() || Decorations.IsEntityAt(Data.Location))
            return;

        Random.InitState(Data.Location.GetHashCode() + Data.Type.GetHashCode());
        float Chance = Random.Range(0f, 1f);
        if (Chance > AmberWeight)
            return;

        Decorations.CreateNewDecoration(DecorationEntity.DType.Amber, Data.Location);
    }

    private DecorationEntity.DType GetTypeToSpawn(HexagonData Data, List<DecorationEntity.DType> SpawnableTypes)
    {
        float TotalWeight = GetSpawnWeight(SpawnableTypes);

        Random.InitState(Data.Location.GetHashCode() + Data.Type.GetHashCode());
        float Chance = Random.Range(0f, TotalWeight);

        foreach (var Type in SpawnableTypes)
        {
            float Weight = GetSpawnWeight(Type);
            if (Chance < Weight)
                return Type;

            Chance -= Weight;
        }

        return DecorationEntity.DType.DEFAULT;
    }

    private float GetSpawnWeight(List<DecorationEntity.DType> SpawnableTypes)
    {
        float Sum = 0;
        foreach (var Type in SpawnableTypes)
        {
            Sum += GetSpawnWeight(Type);
        }
        return Sum;
    }

    private float GetSpawnWeight(DecorationEntity.DType Type)
    {
        switch (Type)
        {
            case DecorationEntity.DType.Altar: return AltarWeight;
            case DecorationEntity.DType.Ruins: return RuinsWeight;
            case DecorationEntity.DType.Tribe: return TribeWeight;
            case DecorationEntity.DType.Relic: return RelicWeight;
            default: return 0;
        }
    }

    private bool CanSpawnTreasure(HexagonConfig.HexagonType Type)
    {
        if (!Game.TryGetServices(out RelicService Relics, out MapGenerator MapGenerator))
            return false;

        int CategoryIndex = MapGenerator.UnlockableTypes.GetCategoryIndexOf(Type);
        if (CategoryIndex >= Relics.UnlockableRelics.GetCategoryCount())
            return false;

        return !Relics.UnlockableRelics.HasCategoryAllUnlocked(CategoryIndex);
    }

    protected override void StopServiceInternal() {}

    public GameObject GetGameObject() { return gameObject; }

    public Vector4 StartLocation;
    public int StartVisibilityRange = 2;
    public int StartScoutingRange = 1;

    [SaveableBaseType]
    private int DiscoveredCount = 0;
    [SaveableBaseType]
    private int CountToSpawnDecoration = 5;
    private int CountToSpawnDecorationIncrease = 3;
    private int MaxCountToSpawn = 15;

    private float TribeWeight = 0.75f;
    private float RuinsWeight = 0.5f;
    private float RelicWeight = 0.2f;
    private float AltarWeight = 0.07f;
    // will be checked for each newly malaised tile, so should be very low
    private float AmberWeight = 0.02f;
}
