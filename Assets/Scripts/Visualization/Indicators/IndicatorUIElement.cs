using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/** Empty UI shell that foces the Indicator to be ignored for clicks */
public class IndicatorUIElement : MonoBehaviour, UIElement
{
    public void Start()
    {
        // @ShouldBeIgnored causes the hover to be swallowed, making buttons ontop not interactable anymore
        gameObject.layer = 0;
    }

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
