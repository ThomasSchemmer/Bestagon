using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/**
 * Interface making selecting, hovering and further interactions with any game element possible
 * Supports both UI Elements and GameWorld Elements 
 * See @Selectors different selector
 * Note: Sometimes a child Selectable wants to pass on events to the parent selectable
 */ 
public interface ISelectable {

    public void SetSelected(bool Selected);

    public void SetHovered(bool Hovered);

    /** Triggers every time the selectable was left-clicked on */
    public void ClickOn(Vector2 PixelPos);

    /** Gets called when the selectable was right-clicked on */
    public void Interact();

    public bool IsEqual(ISelectable other);

    /** Returns true if the element should be selectable and hoverable
     * If false still uses up input
     */
    public bool CanBeInteracted()
    {
        return true;
    }

    public virtual bool ShouldBeIgnored() { return false; }

    /** Invokes the hovering of the parent according to subclass overrides*/
    public void SetHoveredAsParent(bool Hovered) { }
    
    /** Whether the selectable can generally be long-hovered, aka give Tooltips */
    public bool CanBeLongHovered();

    public string GetHoverTooltip() { return "";  }

    /** Returns the fitting selector according to the selectables type */
    public Selector GetSelectorByType()
    {
        if (!Game.TryGetService(out Selectors Selectors))
            return null;

        return Selectors.GetSelectorByType(this);
    }

}