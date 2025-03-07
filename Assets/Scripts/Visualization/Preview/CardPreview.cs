using UnityEngine;

/** Class representing any preview for cards. Usually previews are displayed ontop of the currently hovered hex*/
public abstract class CardPreview : MonoBehaviour
{
    public enum PreviewType
    {
        None,
        Building,
        Unit,
        Event
    }
    
    /** Show the preview over the current hexagon */
    public virtual void Show(HexagonVisualization Hex)
    {
        SetAllowed(Hex);
        //overwritten in subclasses
    }

    protected void SetAllowed(HexagonVisualization Hex)
    {
        SetAllowed(Previewable.IsInteractableWith(Hex, true));
    }

    protected abstract void SetAllowed(bool bIsAllowed);

    /** returns true if the card is for this exact preview (and doesnt have to be remade)*/
    public abstract bool IsFor(Card Card);

    public abstract void Init(Card Card);

    public static CardPreview CreateFor(Card Card)
    {
        GameObject PreviewObject = new("Preview");
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
