using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Draggable : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler, IPointerDownHandler
{
    public void Init()
    {
        RectTransform = gameObject.GetComponent<RectTransform>();
        CanvasGroup = gameObject.GetComponent<CanvasGroup>();
        Canvas = GameObject.Find("UI").GetComponent<Canvas>();
    }

    public void OnDrag(PointerEventData EventData)
    {
        if (!bIsBeingDragged)
            return;

        Vector2 Scale = HexagonConfig.GetScreenScale();
        Vector2 Delta = new(EventData.delta.x / Scale.x, EventData.delta.y / Scale.y);
        RectTransform.anchoredPosition += 0.6f * Delta;
        Manager.Drag(this, EventData);
    }

    public virtual bool CanBeDragged()
    {
        return false;
    }

    public virtual void OnBeginDrag(PointerEventData eventData)
    {
        bIsBeingDragged = CanBeDragged();
        if (!bIsBeingDragged)
            return;

        if (!Game.TryGetService(out Manager))
            return;

        CanvasGroup.alpha = 0.8f;
        CanvasGroup.blocksRaycasts = false;
        OldParent = (RectTransform)transform.parent;
        Manager.BeginDrag(this);
    }

    public void OnEndDrag(PointerEventData EventData)
    {
        if (!bIsBeingDragged)
            return;

        if (!Game.TryGetService(out DraggableManager Manager))
            return;
        
        CanvasGroup.alpha = 1;
        CanvasGroup.blocksRaycasts = true;
        Manager.EndDrag(this, EventData);
    }
    
    public virtual void SetDragParent(RectTransform NewParent, int Index)
    {
        RectTransform Target;
        if (NewParent)
        {
            IDragTarget DragTarget = NewParent.GetComponent<IDragTarget>();
            Target = DragTarget.GetTargetContainer();
        }
        else
        {
            Target = OldParent;
        }
        transform.SetParent(Target, true);
    }

    public void OnPointerDown(PointerEventData eventData) { }

    public Canvas Canvas;

    protected RectTransform RectTransform;
    protected CanvasGroup CanvasGroup;
    protected bool bIsBeingDragged = false;
    protected RectTransform OldParent = null;
    protected DraggableManager Manager;
}
