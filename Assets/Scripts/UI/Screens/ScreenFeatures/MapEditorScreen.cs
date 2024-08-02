using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapEditor : ScreenFeatureGroup
{
    public override bool HasFeatureObject()
    {
        return Game.Instance.Mode == Game.GameMode.MapEditor;
    }

    public void Start()
    {
        Init();
        if (!HasFeatureObject())
        {
            HideFeatures();
        }

    }
}
