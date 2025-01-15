using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/** 
 * Allows embarking a scout onto a boat if its close enough
 */
public class EmbarkScoutScreenFeature : ScreenFeature<UnitEntity>
{
    public Button EmbarkButton;

    public override bool ShouldBeDisplayed()
    {
        if (!TryGetNearbyBoat(out var Boat))
            return false;

        return Boat.LoadedScout == null;
    }

    private bool TryGetNearbyBoat(out BoatEntity Boat)
    {
        Boat = null;
        if (!GetTargetAsTokenized(out var Unit))
            return false;

        if (Unit.UnitType != UnitEntity.UType.Scout)
            return false;

        if (!Game.TryGetService(out Units Units))
            return false;

        if (!Units.TryGetEntityAt(Unit.GetLocations().GetMainLocation(), out var OtherUnit, 1, (int)UnitEntity.UType.Boat))
            return false;

        if (OtherUnit is not BoatEntity)
            return false;

        Boat = OtherUnit as BoatEntity;
        return Boat != null;
    }

    public void Embark()
    {
        if (!GetTargetAsTokenized(out var Unit))
            return;
        if (Unit.UnitType != UnitEntity.UType.Scout)
            return;

        if (!Game.TryGetServices(out Units Units, out Selectors Selectors))
            return;

        if (!TryGetNearbyBoat(out var Boat))
            return;

        Boat.LoadedScout = Unit as ScoutEntity;
        Units.RemoveEntity(Unit);
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
        EmbarkButton.gameObject.SetActive(true);
    }

    public override void Hide()
    {
        base.Hide();
        EmbarkButton.gameObject.SetActive(false);
    }
}
