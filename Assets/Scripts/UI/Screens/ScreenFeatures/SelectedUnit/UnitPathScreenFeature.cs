using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitPathScreenFeature : ScreenFeature<UnitEntity>
{
    public Vector3 Offset = new Vector3(0, 6, 0);
    private LineRenderer LineRenderer;

    public override void Init(ScreenFeatureGroup<UnitEntity> Target)
    {
        base.Init(Target);
        LineRenderer = GetComponent<LineRenderer>();
    }

    public override void ShowAt(float YOffset, float Height)
    {
        base.ShowAt(YOffset, Height);

        TokenizedUnitEntity Unit = GetTargetAsTokenized();
        var Params = Unit.GetPathfindingParams();

        TryGetHexagons(out HexagonVisualization StartHex, out HexagonVisualization TargetHex);
        List<Location> Path = Pathfinding.FindPathFromTo(StartHex.Location, TargetHex.Location, Params);
        List<Location> AffordablePath = Pathfinding.GetAffordableSubPath(Path, Unit.RemainingMovement, Params);

        Vector3[] WorldPositions = new Vector3[AffordablePath.Count];
        for (int i = 0; i < AffordablePath.Count; i++)
        {
            WorldPositions[i] = AffordablePath[i].WorldLocation + Offset;
        }
        LineRenderer.positionCount = WorldPositions.Length;
        LineRenderer.SetPositions(WorldPositions);
    }

    public override bool ShouldBeDisplayed()
    {
        TokenizedUnitEntity Unit = GetTargetAsTokenized();
        return Unit != null && TryGetHexagons(out HexagonVisualization _, out HexagonVisualization _);
    }

    private bool TryGetHexagons(out HexagonVisualization SelectedHex, out HexagonVisualization HoveredHex)
    {
        SelectedHex = null;
        HoveredHex = null;
        if (!Game.TryGetService(out Selectors Selectors))
            return false;

        SelectedHex = Selectors.GetSelectedHexagon();
        HoveredHex = Selectors.GetHoveredHexagon();
        return SelectedHex != null && HoveredHex != null;
    }

    private TokenizedUnitEntity GetTargetAsTokenized()
    {
        return Target.GetFeatureObject() as TokenizedUnitEntity;
    }
}
