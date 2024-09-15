using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public class SpawnSystem : GameService, ISaveableService
{
    protected override void StartServiceInternal()
    {
        Game.RunAfterServicesInit((MapGenerator MapGenerator, CameraController Controller) =>
        {
            if (Game.Instance.State == Game.GameState.CardSelection)
                return;

            InitVisibility();
            TeleportToStart();
            HexagonData._OnDiscoveryStateHex += OnDiscoveredHex;
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

    private void OnDestroy()
    {
        HexagonData._OnDiscoveryStateHex -= OnDiscoveredHex;
    }

    private void OnDiscoveredHex(HexagonData Data, HexagonData.DiscoveryState State)
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
        float Chance = Random.Range(0f, 1f);

        DecorationEntity.DType Decoration = Chance < TribeChance ? 
            DecorationEntity.DType.Tribe : Chance < RuinsChance ?
            DecorationEntity.DType.Ruins :
            DecorationEntity.DType.Treasure;
        DecorationService.CreateNewDecoration(Decoration, Data.Location);
    }

    protected override void StopServiceInternal() {}

    public void Reset() {}

    public int GetSize()
    {
        return sizeof(int) * 2;
    }

    public byte[] GetData()
    {
        NativeArray<byte> Bytes = new(GetSize(), Allocator.Temp);
        int Pos = 0;
        Pos = SaveGameManager.AddInt(Bytes, Pos, DiscoveredCount);
        Pos = SaveGameManager.AddInt(Bytes, Pos, CountToSpawnDecoration);

        return Bytes.ToArray();
    }

    public void SetData(NativeArray<byte> Bytes)
    {
        int Pos = 0;
        Pos = SaveGameManager.GetInt(Bytes, Pos, out DiscoveredCount);
        Pos = SaveGameManager.GetInt(Bytes, Pos, out CountToSpawnDecoration);
    }

    public Vector4 StartLocation;
    public int StartVisibilityRange = 2;
    public int StartScoutingRange = 1;

    private int DiscoveredCount = 0;
    private int CountToSpawnDecoration = 5;
    private int CountToSpawnDecorationIncrease = 3;
    private float TribeChance = 0.45f;
    private float RuinsChance = 0.8f;
}
