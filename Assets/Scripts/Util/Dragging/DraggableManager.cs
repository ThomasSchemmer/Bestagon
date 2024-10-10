using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DraggableManager : GameService
{
    protected override void StartServiceInternal() {
        DraggingPreview = Instantiate(DraggingPreviewPrefab);
        DraggingPreview.SetActive(false);
        DraggingPreview.transform.SetParent(transform, false);
    }

    protected override void StopServiceInternal() {}

    public void BeginDrag(Draggable Draggable)
    {
        DraggedObjects.Add(Draggable);
        Draggable.transform.SetParent(transform, true);
    }

    public void Drag(Draggable Draggable, PointerEventData Event)
    {
        RectTransform Target = GetTarget(Event);
        if (!Target)
            return;

        RectTransform Content = GetContentFromViewport(Target);
        if (!Content)
            return;

        GridLayoutGroup Layout = Content.GetComponent<GridLayoutGroup>();
        if (!Layout)
            return;

        // map pointer to top left inner position of the actual layout
        Vector2 Position = Event.position - new Vector2(Layout.padding.left, Layout.padding.top);
        Position -= new Vector2(Content.position.x, Content.position.y);
        Position.y = -Position.y;
        // then check which index to place at
        Vector2 CellSize = Layout.cellSize + Layout.spacing;
        Vector2Int CellPosition = new();
        CellPosition.x = (int)(Position.x / CellSize.x);
        CellPosition.y = (int)(Position.y / CellSize.y);
        int GlobalPosition = CellPosition.y * Layout.constraintCount + CellPosition.x;
        GlobalPosition = Mathf.Min(GlobalPosition, Content.childCount);

        DraggingPreview.transform.SetParent(Content, true);
        DraggingPreview.transform.SetSiblingIndex(GlobalPosition);
        DraggingPreview.transform.localScale = Vector3.one;
        DraggingPreview.SetActive(true);
    }

    public void EndDrag(Draggable Draggable, PointerEventData Event) 
    {
        if (!DraggedObjects.Contains(Draggable))
            return;

        DraggedObjects.Remove(Draggable);
        int Index = DraggingPreview.transform.GetSiblingIndex();
        Draggable.SetDragParent(GetTarget(Event), Index);
        DraggingPreview.SetActive(false);
        DraggingPreview.transform.SetParent(transform, false);

        if (Draggable is Card)
        {
            Card DraggableCard = (Card)Draggable;
            DraggableCard.SetPinned(DraggableCard.IsPinned() ? Index : -1);
        }

        if (!Game.TryGetService(out CardUpgradeScreen Screen))
            return;

        Screen.HideCardButtons();
    }

    private RectTransform GetTarget(PointerEventData Event)
    {
        RectTransform Chosen = null;
        foreach (RectTransform Target in Targets)
        {
            if (GetWorldRect(Target).Contains(Event.position))
            {
                Chosen = Target;
                break;
            }
        }
        return Chosen;
    }
    private Rect GetWorldRect(RectTransform Rect)
    {
        Vector3[] Vertices = new Vector3[4];
        Rect.GetWorldCorners(Vertices);
        Vector3 Position = Vertices[0];

        Vector2 Size = new Vector2(
            Rect.lossyScale.x * Rect.rect.size.x,
            Rect.lossyScale.y * Rect.rect.size.y);

        return new Rect(Position, Size);
    }

    public bool IsDragging()
    {
        return DraggedObjects.Count > 0;
    }

    public RectTransform GetContentFromViewport(RectTransform Viewport)
    {
        // we have to check the viewport, not the container
        // the container height will change according to content, so mouse events will be invalid!
        return Viewport.childCount > 0 ? Viewport.GetChild(0).GetComponent<RectTransform>() : null;
    }

    public List<RectTransform> Targets = new();
    public GameObject DraggingPreviewPrefab;

    private GameObject DraggingPreview;
    private List<Draggable> DraggedObjects = new();
}
