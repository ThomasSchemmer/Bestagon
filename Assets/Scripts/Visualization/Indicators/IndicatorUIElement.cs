using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/** Empty UI shell that foces the Indicator to be ignored for clicks */
public class IndicatorUIElement : MonoBehaviour, UIElement
{
    public bool CanBeLongHovered()
    {
        return false;
    }

    public void ClickOn(Vector2 PixelPos) {}

    public void Interact() { }

    public bool IsEqual(ISelectable other)
    {
        return false;
    }

    public void SetHovered(bool Hovered) { }

    public void SetSelected(bool Selected) { }

    public bool ShouldBeIgnored()
    {
        return true;
    }
}
