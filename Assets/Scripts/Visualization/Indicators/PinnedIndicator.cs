using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Card))]
public class PinnedIndicator : SpriteIndicatorComponent
{
    private Card Card;

    protected override void Initialize()
    {
        Card = GetComponent<Card>();
    }

    protected override int GetTargetLayer()
    {
        return 0;
    }

    protected override int GetIndicatorAmount()
    {
        return 1;
    }

    protected override Sprite GetIndicatorSprite(int i)
    {
        if (!Game.TryGetService(out IconFactory IconFactory))
            return null;

        return IconFactory.GetIconForMisc(IconFactory.MiscellaneousType.PinActive);
    }

    protected override void ApplyIndicatorScreenPosition(int i, RectTransform IndicatorTransform)
    {
        RectTransform RectTransform = Card.GetComponent<RectTransform>();
        Vector3 TargetPosition = RectTransform.position;
        TargetPosition.x += 200 / 2f - 15;
        TargetPosition.y += 320 / 2f - 15;
        Vector3 OffsetWorld = TargetPosition - IndicatorTransform.position;
        Vector3 OffsetLocal = IndicatorTransform.InverseTransformVector(OffsetWorld);
        IndicatorTransform.anchoredPosition = IndicatorTransform.anchoredPosition + (Vector2)OffsetLocal;
    }

    public override bool NeedsVisualUpdate()
    {
        // static image, the "negative" is simply hidden
        return false;
    }
}
