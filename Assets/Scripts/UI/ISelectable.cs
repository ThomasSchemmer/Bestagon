using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ISelectable {

    public void SetSelected(bool Selected);

    public void SetHovered(bool Hovered);

    public void ClickOn(Vector2 PixelPos);

    public void Interact();

    public bool IsEqual(ISelectable other);

    public bool CanBeInteracted()
    {
        return true;
    }

    public bool CanBeLongHovered();

    public string GetHoverTooltip() { return "";  }

    public Selector GetSelectorFor(ISelectable Selectable)
    {
        if (!Game.TryGetService(out Selectors Selectors))
            return null;

        return Selectors.GetSelectorByType(Selectable);
    }

}