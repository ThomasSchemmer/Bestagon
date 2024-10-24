using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/**
 * Designates a UI component to be a valid target for dragging as well as specifies which exact component should be dropped into
 */
public interface IDragTarget
{
    /** Returns the rect that should be checked for size */
    public RectTransform GetSizeRect();

    /** Returns the container the draggable will end up in */
    public RectTransform GetTargetContainer();

    public int GetTargetSiblingIndex(PointerEventData Event);
}
