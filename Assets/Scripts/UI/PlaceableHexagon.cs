using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
[RequireComponent(typeof(Image))]
/** 
 * UI element that when selected enables overriding hexagons in the world map with its type
 */
public class PlaceableHexagon : MonoBehaviour, UIElement
{
    public HexagonConfig.HexagonType Type;
    public HexagonConfig.HexagonHeight Height = HexagonConfig.HexagonHeight.Flat;

    public void ClickOn(Vector2 PixelPos) { }

    public void Interact() {}

    public bool IsEqual(Selectable other)
    {
        PlaceableHexagon OtherHex = other as PlaceableHexagon;
        if (OtherHex == null)
            return false;

        return Type == OtherHex.Type;
    }

    public void SetHovered(bool Hovered){
        float Scale = IsSelected ? SELECT_SIZE :
                                Hovered ? HOVER_SIZE : UNSELECT_SIZE;
        transform.localScale = Vector3.one * Scale;
    }

    public void SetSelected(bool Selected){
        if (!Game.TryGetService(out Selector Selector))
            return;
        Selector.DeselectHexagon();

        IsSelected = Selected;
        transform.localScale = Vector3.one * (Selected ? SELECT_SIZE : UNSELECT_SIZE);
    }

    public void Init(HexagonConfig.HexagonType Type)
    {
        this.Type = Type;
        gameObject.layer = LayerMask.NameToLayer("UI");
        transform.localScale = Vector3.one * UNSELECT_SIZE;
        Image Image = GetComponent<Image>();
        Image.sprite = Resources.Load<Sprite>("Pictures/" + Type);
        Image.rectTransform.sizeDelta = new Vector2(SIZE, SIZE);
    }

    private bool IsSelected = false;

    private static float SELECT_SIZE = 1;
    private static float UNSELECT_SIZE = 0.75f;
    private static float HOVER_SIZE = 0.85f;
    public static int SIZE = 150;
    public static int OFFSET = 25;
}
