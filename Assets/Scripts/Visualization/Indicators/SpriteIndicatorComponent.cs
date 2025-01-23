using System.Collections;
using System.Collections.Generic;
using Unity.VectorGraphics;
using UnityEngine;

/** Represents an Indicator that is a single sprite */
public abstract class SpriteIndicatorComponent : IndicatorComponent
{
    protected abstract Sprite GetIndicatorSprite(int i);

    protected override void UpdateIndicatorVisuals(int i)
    {
        if (Indicators[i] == null)
            return;

        SVGImage Image = Indicators[i].GetComponent<SVGImage>();
        Image.sprite = GetIndicatorSprite(i);
        Indicators[i].localScale = HexagonConfig.GetScreenScale();
    }

    public override IndicatorService.IndicatorType GetIndicatorType()
    {
        return IndicatorService.IndicatorType.Sprite;
    }

    protected override GameObject InstantiateIndicator(int i, RectTransform Parent)
    {
        if (!Game.TryGetService(out IconFactory IconFactory))
            return null;

        return IconFactory.GetVisualsForSpriteIndicator(Parent);
    }
}
