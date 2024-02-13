using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

public class DraggableManager : GameService
{
    protected override void StartServiceInternal() {}

    protected override void StopServiceInternal() {}

    public void BeginDrag(Draggable Draggable)
    {
        DraggedObjects.Add(Draggable);
        Draggable.transform.SetParent(transform, true);
    }

    public void Drag(Draggable Draggable, PointerEventData Event)
    {
        // todo: use to create dragging target feedback
    }

    public void EndDrag(Draggable Draggable, PointerEventData Event) 
    {
        Draggable.SetDragParent(GetTarget(Event));

        if (!Game.TryGetService(out CardUpgradeScreen Screen))
            return;

        Screen.HideUpgradeButton();
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

    public List<RectTransform> Targets = new();

    private List<Draggable> DraggedObjects = new();
}
