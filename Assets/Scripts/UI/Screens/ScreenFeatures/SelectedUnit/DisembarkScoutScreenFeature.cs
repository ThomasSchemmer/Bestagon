using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/** 
 * Allows disembarking a scout from a loaded boat if enough land is around
 */
public class DisembarkScoutScreenFeature : ScreenFeature<UnitEntity>
{
    public Button DisembarkButton;

    public override bool ShouldBeDisplayed()
    {
        return TryGetLandingSpot(out var _);
    }

    private bool TryGetLandingSpot(out HexagonVisualization Target)
    {
        Target = null;
        if (!GetTargetAsTokenized(out var Unit))
            return false;

        if (Unit.UnitType != UnitEntity.UType.Boat)
            return false;

        BoatEntity Boat = Unit as BoatEntity;
        if (Boat == null)
            return false;

        if (Boat.LoadedScout == null)
            return false;

        if (!Game.TryGetServices(out MapGenerator MapGen, out Units Units))
            return false;

        var Neighbours = MapGen.GetNeighbours(Boat.GetLocations(), false);
        foreach (var Neighbour in Neighbours)
        {
            if (Neighbour.Data.Type == HexagonConfig.HexagonType.Ocean || Neighbour.Data.Type == HexagonConfig.HexagonType.DeepOcean)
                continue;

            if (!ScoutEntity._IsInteractableWith(Neighbour, false))
                continue;

            Target = Neighbour;
            return true;
        }
        return false;
    }

    public void Disembark()
    {
        if (!TryGetLandingSpot(out var Target)) 
            return;

        if (!Game.TryGetServices(out Units Units, out Selectors Selectors))
            return;

        if (!GetTargetAsTokenized(out var Unit))
            return;

        BoatEntity Boat = Unit as BoatEntity;
        if (Boat == null) 
            return;

        Boat.LoadedScout.MoveTo(Target.Location, 0);
        Units.AddUnit(Boat.LoadedScout);
        Boat.LoadedScout = null;
        Selectors.DeselectHexagon();
    }

    private bool GetTargetAsTokenized(out TokenizedUnitEntity Unit)
    {
        Unit = Target.GetFeatureObject() as TokenizedUnitEntity;
        return Unit != null;
    }

    public override void ShowAt(float YOffset, float Height)
    {
        base.ShowAt(YOffset, Height);
        DisembarkButton.gameObject.SetActive(true);
    }

    public override void Hide()
    {
        base.Hide();
        DisembarkButton.gameObject.SetActive(false);
    }
}
