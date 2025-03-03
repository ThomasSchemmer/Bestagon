using System.Collections.Generic;
using UnityEngine;


/** Group of ScreenFeatures to automatically fill in a UI element
 * Since most UI element's depend on other UI elements for position, it has to support callbacks
 */

public abstract class ScreenFeatureGroup : MonoBehaviour {
    public delegate void OnLayoutChanged();
    public OnLayoutChanged _OnLayoutChanged;

    public RectTransform PreviousTransform;
    public float Margin;
    public float Padding;

    public List<ScreenFeature> ScreenFeatures;
    public float OffsetBetweenElements = 10;

    private RectTransform RectTransform;
    private ScreenFeatureGroup PreviousGroup;

    protected float ConditionalPadding = 0;

    public float GetConditionalPadding()
    {
        return ConditionalPadding;
    }

    public void SetConditionalPadding(float Padding)
    {
        ConditionalPadding = Padding;
        _OnLayoutChanged?.Invoke();
    }


    public void Init()
    {
        if (TryGetPrevGroup(PreviousTransform, out var PrevGroup))
        {
            PrevGroup._OnLayoutChanged += UpdateLayout;
        }

        RectTransform = GetComponent<RectTransform>();
        InitInternal();

        // otherwise the chain will trigger it anyway
        if (PreviousTransform == null)
        {
            UpdateLayout();
        }
    }

    protected virtual void InitInternal()
    {
        foreach (ScreenFeature Feature in ScreenFeatures)
        {
            Feature.Init();
        }
    }

    public void UpdateLayout()
    {
        float OverallHeight = GetOverallHeight();
        RectTransform.sizeDelta = new Vector2(RectTransform.sizeDelta.x, OverallHeight);

        RectTransform TempPrev = GetPreviousTransform();
        if (TempPrev != null)
        {
            RectTransform.anchoredPosition = new Vector2(
                RectTransform.anchoredPosition.x,
                TempPrev.anchoredPosition.y + TempPrev.sizeDelta.y / 2 + OverallHeight / 2 - GetMargin()
            );
        }
        _OnLayoutChanged?.Invoke();
    }

    private float GetMargin()
    {
        return GetOffset(true);
    }

    private float GetPadding()
    {
        return GetOffset(false);
    }

    private float GetOffset(bool bIsMargin)
    {

        float CurrentValue = bIsMargin ? Margin : Padding;
        RectTransform PrevTransform = PreviousTransform;

        while (TryGetPrevGroup(PrevTransform, out var PrevGroup))
        {
            if (PrevGroup.gameObject.activeSelf)
                return CurrentValue;

            CurrentValue = bIsMargin ? PrevGroup.Margin : PrevGroup.Padding;
            PrevTransform = PrevGroup.PreviousTransform;
            PrevGroup = PrevTransform.GetComponent<ScreenFeatureGroup>();
        }

        return CurrentValue;
    }

    private RectTransform GetPreviousTransform()
    {
        RectTransform PrevTransform = PreviousTransform;
        while (TryGetPrevGroup(PrevTransform, out var PrevGroup))
        {
            if (PrevGroup.gameObject.activeSelf)
                return PrevTransform;

            PrevTransform = PrevGroup.PreviousTransform;
            PrevGroup = PrevTransform.GetComponent<ScreenFeatureGroup>();
        }

        return PrevTransform;
    }

    private bool TryGetPrevGroup(RectTransform PreviousTransform, out ScreenFeatureGroup PrevGroup)
    {
        PrevGroup = default;
        RectTransform PrevTransform = PreviousTransform;
        if (PrevTransform == null)
            return false;

        PrevGroup = PrevTransform.GetComponent<ScreenFeatureGroup>();
        return PrevGroup != null;
    }

    public void ShowFeatures()
    {
        if (!ShouldFeatureGroupBeDisplayed())
            return;

        gameObject.SetActive(true);
        UpdateLayout();

        float YOffset = 0;
        float PrevHeight = 0;
        foreach (ScreenFeature Feature in ScreenFeatures)
        {
            if (!Feature.ShouldBeDisplayed())
            {
                Feature.Hide();
                continue;
            }

            float CurrentHeight = Feature.GetHeight();
            YOffset = YOffset - OffsetBetweenElements - PrevHeight / 2.0f - CurrentHeight / 2.0f;
            Feature.ShowAt(YOffset, CurrentHeight);
            PrevHeight = CurrentHeight;
        }
    }

    private bool ShouldFeatureGroupBeDisplayed()
    {
        foreach (ScreenFeature Feature in ScreenFeatures)
        {
            if (Feature.ShouldBeDisplayed())
                return true;
        }
        return false;
    }

    private float GetOverallHeight()
    {
        float Height = 0;
        foreach (ScreenFeature Feature in ScreenFeatures)
        {
            if (!Feature.ShouldBeDisplayed())
                continue;

            Height += Feature.GetHeight();
        }
        float PreviousPadding = TryGetPrevGroup(PreviousTransform, out var PrevGroup) ? PrevGroup.GetConditionalPadding() : 0;
        Height += OffsetBetweenElements * (ScreenFeatures.Count - 1) + GetPadding() + PreviousPadding;
        return Height;
    }

    public void HideFeatures()
    {
        foreach (ScreenFeature Feature in ScreenFeatures)
        {
            Feature.Hide();
        }
        gameObject.SetActive(false);
        UpdateLayout();
    }

    public abstract bool HasFeatureObject();
}

public abstract class ScreenFeatureGroup<T> : ScreenFeatureGroup
{
    public abstract T GetFeatureObject();

    protected override void InitInternal()
    {
        foreach (ScreenFeature<T> Feature in ScreenFeatures)
        {
            Feature.Init(this);
        }
    }
}
