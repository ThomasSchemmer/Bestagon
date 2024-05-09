using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnSystem : GameService
{
    protected override void StartServiceInternal()
    {
        Game.RunAfterServicesInit((MapGenerator MapGenerator, CameraController Controller) =>
        {
            Location TargetLocation = new Location((int)StartLocation.x, (int)StartLocation.y, (int)StartLocation.z, (int)StartLocation.w);
            if (!MapGenerator.TryGetHexagon(TargetLocation, out HexagonVisualization Hex))
                return;

            Hex.UpdateDiscoveryState(StartVisibilityRange, StartScoutingRange);
            Controller.TeleportTo(TargetLocation.WorldLocation);
        });
    }

    protected override void StopServiceInternal() {}

    public Vector4 StartLocation;
    public int StartVisibilityRange = 2;
    public int StartScoutingRange = 1;
}
