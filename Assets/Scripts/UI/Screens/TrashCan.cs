using System.Collections;
using System.Collections.Generic;
using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TrashCan : MonoBehaviour, IDragTarget, UIElement
{
    private Image BackgroundImage;
    private SVGImage CanImage;

    public void Start()
    {
        BackgroundImage = GetComponent<Image>();
        CanImage = transform.GetChild(0).GetComponent<SVGImage>();
        SetHovered(false);
    }

    public bool CanBeLongHovered()
    {
        return true;
    }

    public void ClickOn(Vector2 PixelPos) {}

    public RectTransform GetSizeRect()
    {
        return GetComponent<RectTransform>();
    }

    public RectTransform GetTargetContainer()
    {
        return GetComponent<RectTransform>();
    }

    public int GetTargetSiblingIndex(PointerEventData Event)
    {
        return 0;
    }

    public void Interact() { }

    public bool IsEqual(ISelectable other)
    {
        return other is TrashCan;
    }

    public void SetHovered(bool bIsHovered)
    {
        if (!Game.TryGetService(out IconFactory IconFactory))
            return;

        BackgroundImage.color = bIsHovered ? HoverColor : NormalColor;
        var Type = bIsHovered ? IconFactory.MiscellaneousType.TrashCanOpen : IconFactory.MiscellaneousType.TrashCan;
        CanImage.sprite = IconFactory.GetIconForMisc(Type);
    }

    public void SetSelected(bool Selected) { }


    public static Color HoverColor = new(1, 1, 1, 0.8f);
    public static Color NormalColor = new(1, 1, 1, 0.6f);
}
