using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DogPettingScreenFeature : ScreenFeature<UnitEntity>
{
    public Button PetButton;

    public override bool ShouldBeDisplayed()
    {
        UnitEntity Unit = Target.GetFeatureObject();
        if (Unit == null)
            return false;

        if (Unit.UnitType != UnitEntity.UType.Scout)
            return false;

        ScoutEntity Scout = Unit as ScoutEntity;
        if (Scout == null) 
            return false;

        return Scout.HasDog();
    }

    public override void ShowAt(float YOffset, float Height)
    {
        base.ShowAt(YOffset, Height);
        PetButton.gameObject.SetActive(true);
    }

    public override void Hide()
    {
        base.Hide();
        PetButton.gameObject.SetActive(false);
    }
}
