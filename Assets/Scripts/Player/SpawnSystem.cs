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

            Location TargetLocation = new Location((int)StartLocation.x, (int)StartLocation.y, (int)StartLocation.z, (int)StartLocation.w);
            if (!MapGenerator.TryGetHexagon(TargetLocation, out HexagonVisualization Hex))
                return;

            Hex.UpdateDiscoveryState(StartVisibilityRange, StartScoutingRange);
            Controller.TeleportTo(TargetLocation.WorldLocation);
            HexagonData._OnDiscoveryStateHex += OnDiscoveredHex;
        });
    }

    private void OnDestroy()
    {
        HexagonData._OnDiscoveryStateHex -= OnDiscoveredHex;
    }

    private void OnDiscoveredHex(HexagonData Data, HexagonData.DiscoveryState State)
    {
        if (State != HexagonData.DiscoveryState.Visited)
            return;

        DiscoveredCount++;
        if (DiscoveredCount < CountToSpawnDecoration)
            return;

        if (!Data.CanDecorationSpawn())
            return;

        DiscoveredCount -= CountToSpawnDecoration;
        CountToSpawnDecoration += CountToSpawnDecorationIncrease;
        float Chance = Random.Range(0f, 1f);
        HexagonConfig.HexagonDecoration Decoration = Chance < TribeChance ? 
            HexagonConfig.HexagonDecoration.Tribe : 
            HexagonConfig.HexagonDecoration.Ruins;
        Data.Decoration = Decoration;
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
    private float TribeChance = 0.65f;
}
