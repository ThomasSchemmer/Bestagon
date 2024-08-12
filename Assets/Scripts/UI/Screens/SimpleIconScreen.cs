using System.Collections;
using System.Collections.Generic;
using Unity.VectorGraphics;
using UnityEngine;

/** 
 * Class representing a single icon being rendered as a UI Element
 * Can be contained in another UIElement and trigger its callbacks
 */
public class SimpleIconScreen : MonoBehaviour, UIElement
{
    public string HoverTooltip;

    protected ISelectable Parent;
    protected SVGImage IconRenderer;

    protected bool bIsEnabled = true, bIsIgnored = false;

    public bool CanBeLongHovered()
    {
        return true;
    }

    public void ClickOn(Vector2 PixelPos) {
        if (Parent == null)
            return;

        Parent.ClickOn(PixelPos);   
    }

    public virtual void Initialize(Sprite Sprite, string HoverTooltip, ISelectable Parent)
    {
        this.Parent = Parent;
        this.HoverTooltip = HoverTooltip;
        Initialize(Sprite);
    }

    private void Initialize(Sprite Sprite)
    {
        Refresh();
        IconRenderer.sprite = Sprite;
        float x = IconRenderer.transform.localPosition.x;
        IconRenderer.transform.localPosition = new Vector3(x, 0, 0);
    }

    public void Refresh()
    {
        IconRenderer = transform.GetChild(0).GetComponent<SVGImage>();
    }

    public void Interact() {}

    public bool IsEqual(ISelectable other)
    {
        SimpleIconScreen OtherIcon = other as SimpleIconScreen;
        if (OtherIcon == null)
            return false;

        return IconRenderer.sprite.Equals(OtherIcon.IconRenderer.sprite);
    }

    public void SetHovered(bool Hovered) {
        if (Parent == null)
            return;

        Parent.SetHoveredAsParent(Hovered);
    }

    public void SetSelected(bool Selected)
    {
        if (Parent == null)
            return;

        Parent.GetSelectorByType().SetSelected(Parent, Selected);
    }

    public string GetHoverTooltip()
    {
        if (Parent != null && Parent is not Card)
            return Parent.GetHoverTooltip();

        return HoverTooltip;
    }
    public virtual void SetSelectionEnabled(bool bEnabled)
    {
        bIsEnabled = bEnabled;
        IconRenderer.gameObject.layer = bIsEnabled ? LayerMask.NameToLayer(Selectors.UILayerName) : 0;
    }

    public void SetIgnored(bool bIgnored)
    {
        bIsIgnored = bIgnored;
    }

    public bool ShouldBeIgnored() { return bIsIgnored; }
}
