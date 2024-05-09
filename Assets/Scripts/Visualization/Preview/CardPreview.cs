using System.Collections;
using System.Collections.Generic;
using System.Drawing.Printing;
using UnityEngine;

public abstract class CardPreview : MonoBehaviour
{
    public enum PreviewType
    {
        None,
        Building,
        Unit,
        Event
    }

    public virtual void Show(HexagonVisualization Hex)
    {
        SetAllowed(Hex);
        //overwritten in subclasses
    }

    protected void SetAllowed(HexagonVisualization Hex)
    {
        SetAllowed(Previewable.CanBeInteractedOn(Hex));
    }

    protected abstract void SetAllowed(bool bIsAllowed);

    public abstract bool IsFor(Card Card);

    public abstract void Init(Card Card);

    public static CardPreview CreateFor(Card Card)
    {
        GameObject PreviewObject = new();
        CardPreview Preview = null;
        if (Card is BuildingCard)
            Preview = PreviewObject.AddComponent<BuildingPreview>();
        if (Card is EventCard)
            Preview = (Card as EventCard).AddPreviewByType(PreviewObject);

        if (Preview != null)
        {
            Preview.Init(Card);
        }
        
        return Preview;
    }

    public IPreviewable GetPreviewable()
    {
        return Previewable;
    }

    public T GetPreviewableAs<T>() where T : IPreviewable
    {
        if (Previewable is not T)
            return default;

        return (T)Previewable;
    }

    protected IPreviewable Previewable;
    protected GameObject PreviewObject;
}
