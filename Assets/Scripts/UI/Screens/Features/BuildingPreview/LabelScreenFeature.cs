using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LabelScreenFeature : ScreenFeature<MeshPreview>
{
    public override bool ShouldBeDisplayed()
    {
        return Target.GetFeatureObject() != null;
    }
}
