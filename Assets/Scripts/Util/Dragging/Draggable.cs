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

    public void OnDrag(PointerEventData eventData)
    {
        if (!bIsBeingDragged)
            return;

        RectTransform.anchoredPosition += eventData.delta;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        bIsBeingDragged = Game.IsDraggingAllowed();
        if (!bIsBeingDragged)
            return;

        if (!Game.TryGetService(out DraggableManager Manager))
            return;

        CanvasGroup.alpha = 0.6f;
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
        HandleHighlight();
    }

    public void SetDragParent(RectTransform NewParent)
    {
        RectTransform Target;
        if (NewParent)
        {
            // we are targeting the container, not the viewport
            Target = NewParent.transform.GetChild(0)?.GetComponent<RectTransform>();
        }
        else
        {
            Target = OldParent;
        }
        transform.SetParent(Target, true);
    }

    public void OnPointerDown(PointerEventData eventData) { }

    private void HandleHighlight()
    {
        GameObject Parent = transform.parent.gameObject;
        if (Parent == null) 
            return;

        CardContainerUI UI = Parent.GetComponent<CardContainerUI>();
        if (UI == null)
            return;

        CanvasGroup.alpha = UI.bIsActiveCards ? 1 : 0.8f;
    }


    public Canvas Canvas;

    protected RectTransform RectTransform;
    protected CanvasGroup CanvasGroup;
    protected bool bIsBeingDragged = false;
    protected RectTransform OldParent = null;
}
