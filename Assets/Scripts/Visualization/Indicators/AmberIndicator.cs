using System.Collections;
using System.Collections.Generic;
using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.Profiling;

/** Represents an Indicator that shows the location of an amber */
[RequireComponent(typeof(DecorationVisualization))]
public class AmberIndicator : SpriteIndicatorComponent
{
    private DecorationVisualization Visualization;

    protected override void Initialize()
    {
        Visualization = GetComponent<DecorationVisualization>();
    }

    protected override int GetTargetLayer()
    {
        return LayerMask.NameToLayer("UI");
    }

    protected override int GetIndicatorAmount()
    {
        return 1;
    }

    protected override Sprite GetIndicatorSprite(int i)
    {
        if (!Game.TryGetService(out IconFactory IconFactory))
            return null;

        return IconFactory.GetIconForMisc(IconFactory.MiscellaneousType.Amber);
    }

    protected override void ApplyIndicatorScreenPosition(int i, RectTransform IndicatorTransform)
    {
        Location Location = Visualization.Entity.GetLocations().GetMainLocation();
        Vector3 WorldPos = HexagonConfig.TileSpaceToWorldSpace(Location.GlobalTileLocation);
        IndicatorTransform.position = Service.WorldPosToScreenPos(WorldPos);
    }

    public override bool IsFor(LocationSet Location)
    {
        return Visualization.Entity.GetLocations().Equals(Location);
    }

    public override bool NeedsVisualUpdate()
    {
        // static image
        return false;
    }
    protected override void UpdateIndicatorVisuals(int i)
    {
        base.UpdateIndicatorVisuals(i);
        if (Indicators[i] == null)
            return;

        RectTransform Rect = Indicators[i].GetComponent<RectTransform>();
        Rect.sizeDelta = Size;
    }


    public static Vector3 Size = new(50, 75, 1);
}
