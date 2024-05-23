using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitPathScreenFeature : ScreenFeature<UnitData>
{
    public Vector3 Offset = new Vector3(0, 6, 0);
    private LineRenderer LineRenderer;

    public override void Init(ScreenFeatureGroup<UnitData> Target)
    {
        base.Init(Target);
        LineRenderer = GetComponent<LineRenderer>();
    }

    public override void ShowAt(float YOffset)
    {
        base.ShowAt(YOffset);

        TryGetHexagons(out HexagonVisualization StartHex, out HexagonVisualization TargetHex);
        List<Location> Path = Pathfinding.FindPathFromTo(StartHex.Location, TargetHex.Location);

        Vector3[] WorldPositions = new Vector3[Path.Count];
        for (int i = 0; i < Path.Count; i++)
        {
            WorldPositions[i] = Path[i].WorldLocation + Offset;
        }
        LineRenderer.positionCount = WorldPositions.Length;
        LineRenderer.SetPositions(WorldPositions);
    }

    public override bool ShouldBeDisplayed()
    {
        TokenizedUnitData Unit = GetTargetAsTokenized();
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

    private TokenizedUnitData GetTargetAsTokenized()
    {
        return Target.GetFeatureObject() as TokenizedUnitData;
    }
}
