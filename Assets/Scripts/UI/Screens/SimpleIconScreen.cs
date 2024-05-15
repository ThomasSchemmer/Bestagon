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

    public bool CanBeLongHovered()
    {
        return true;
    }

    public void ClickOn(Vector2 PixelPos) {
        if (Parent == null)
            return;

        Parent.ClickOn(PixelPos);   
    }

    public virtual void Initialize(Sprite Sprite, bool bShowRegular, string HoverTooltip, ISelectable Parent)
    {
        this.Parent = Parent;
        this.HoverTooltip = HoverTooltip;
        Initialize(Sprite, bShowRegular);
    }

    private void Initialize(Sprite Sprite, bool bShowRegular)
    {
        IconRenderer = transform.GetChild(0).GetComponent<SVGImage>();
        IconRenderer.sprite = Sprite;
        float x = IconRenderer.transform.localPosition.x;
        IconRenderer.transform.localPosition = new Vector3(bShowRegular ? x : -x, 0, 0);
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
        IconRenderer.gameObject.layer = bEnabled ? LayerMask.NameToLayer(Selectors.UILayerName) : 0;
    }
}
