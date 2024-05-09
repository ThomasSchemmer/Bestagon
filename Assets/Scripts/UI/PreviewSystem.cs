using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PreviewSystem : GameService
{
    public void Hide() {
        if (!IsInit)
            return;

        Reset();
        _OnPreviewHidden?.Invoke();
    }

    private void Reset()
    {
        if (Preview == null)
            return;
        
        Destroy(Preview.gameObject);
        Preview = null;
    }

    public void Show(Card Card, HexagonVisualization Hex) {
        if (!IsInit)
            return;

        SetPreview(Card);

        Preview.Show(Hex);

        _OnPreviewShown?.Invoke();
    }

    private void SetPreview(Card Card)
    {
        if (Preview != null && Preview.IsFor(Card))
            return;

        Preview = CardPreview.CreateFor(Card);
    }

    protected override void StartServiceInternal()
    {
        _OnInit?.Invoke();
    }

    public T GetPreviewableAs<T>() where T : IPreviewable
    {
        if (Preview == null)
            return default;

        return Preview.GetPreviewableAs<T>();
    }

    protected override void StopServiceInternal() {}

    public Material PreviewMaterial;

    private CardPreview Preview;

    public delegate void OnPreviewShown();
    public delegate void OnPreviewHidden();
    public OnPreviewShown _OnPreviewShown;
    public OnPreviewHidden _OnPreviewHidden;
}
