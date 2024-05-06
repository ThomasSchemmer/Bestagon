using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/** 
 * Convenience wrapper to allow usage of Unity Buttons with connection to the custom tooltip generator system
 */
public class HoverableButton : Button, UIElement
{
    public bool CanBeHovered()
    {
        return true;
    }

    public void ClickOn(Vector2 PixelPos){}

    public void Interact() {}

    public bool IsEqual(Selectable other){
        if (other is not HoverableButton)
            return false;

        return name.Equals((other as HoverableButton).name);
    }

    public void SetHovered(bool Hovered){}

    public void SetSelected(bool Selected){}

    public string GetHoverTooltip()
    {
        return HoverText;
    }

    public string HoverText;
}
