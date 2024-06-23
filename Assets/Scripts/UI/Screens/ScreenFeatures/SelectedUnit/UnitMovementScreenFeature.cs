using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitMovementScreenFeature : ScreenFeature<UnitData>
{
    public Color ColorAllowed;
    public Color ColorForbidden;
    public override bool ShouldBeDisplayed()
    {
        TokenizedUnitData Unit = GetTargetAsTokenized();
        return Unit != null && TryGetHexagons(out HexagonVisualization _, out HexagonVisualization _);
    }

    public override void ShowAt(float YOffset)
    {
        base.ShowAt(YOffset);
        Game.TryGetService(out Selectors Selectors);
        TokenizedUnitData UnitData = GetTargetAsTokenized();

        TryGetHexagons(out HexagonVisualization StartHex, out HexagonVisualization TargetHex);
        List<Location> Path = Pathfinding.FindPathFromTo(StartHex.Location, TargetHex.Location);
        int Costs = Pathfinding.GetCostsForPath(Path);
        int Remaining = UnitData.RemainingMovement;
        Color Color = Costs > Remaining || Costs < 0 ? ColorForbidden : ColorAllowed;

        TargetText.text = Costs + "/" + Remaining + " moves";
        TargetText.color = Color;
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
