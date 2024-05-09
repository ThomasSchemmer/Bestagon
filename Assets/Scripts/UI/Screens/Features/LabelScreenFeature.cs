using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LabelScreenFeature : ScreenFeature<PreviewSystem>
{
    public override bool ShouldBeDisplayed()
    {
        return Target.GetFeatureObject() != null;
    }
}
