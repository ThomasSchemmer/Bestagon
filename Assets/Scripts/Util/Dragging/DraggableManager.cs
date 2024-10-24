using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

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
        RectTransform Target = GetDragTarget(Event);
        if (!Target)
            return;

        IDragTarget DragTarget = Target.GetComponent<IDragTarget>();

        RectTransform Content = DragTarget.GetTargetContainer();
        if (!Content)
            return;

        int SiblingIndex = DragTarget.GetTargetSiblingIndex(Event);

        DraggingPreview.transform.SetParent(Content, true);
        DraggingPreview.transform.SetSiblingIndex(SiblingIndex);
        DraggingPreview.transform.localScale = Vector3.one;
        DraggingPreview.SetActive(true);
    }

    public void EndDrag(Draggable Draggable, PointerEventData Event) 
    {
        if (!DraggedObjects.Contains(Draggable))
            return;

        DraggedObjects.Remove(Draggable);
        int Index = DraggingPreview.transform.GetSiblingIndex();
        Draggable.SetDragParent(GetDragTarget(Event), Index);
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

    private RectTransform GetDragTarget(PointerEventData Event)
    {
        RectTransform Chosen = null;
        foreach (RectTransform Rect in Targets)
        {
            IDragTarget DragTarget = Rect.GetComponent<IDragTarget>();
            if (DragTarget == null)
                continue;

            // the checked size rect does not need to be the same as the target container
            if (GetWorldRect(DragTarget.GetSizeRect()).Contains(Event.position))
            {
                Chosen = DragTarget.GetTargetContainer();
                break;
            }
        }
        return Chosen;
    }
    private Rect GetWorldRect(RectTransform Rect)
    {
        if (Rect == null)
            return new(-1, -1, -1, -1);

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

    public List<RectTransform> Targets = new();
    public GameObject DraggingPreviewPrefab;

    private GameObject DraggingPreview;
    private List<Draggable> DraggedObjects = new();
}
