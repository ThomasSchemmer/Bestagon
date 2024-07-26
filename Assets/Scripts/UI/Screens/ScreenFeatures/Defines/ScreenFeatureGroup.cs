using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.UI;

public abstract class ScreenFeatureGroup : MonoBehaviour {
    public delegate void OnLayoutChanged();
    public OnLayoutChanged _OnLayoutChanged;

    public RectTransform PreviousTransform;
    public float Margin;
    public float Padding;

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

}


/** Group of ScreenFeatures to automatically fill in a UI element
 * Since most UI element's depend on other UI elements for position, it has to support callbacks
 */
public abstract class ScreenFeatureGroup<T> : ScreenFeatureGroup
{
    public List<ScreenFeature<T>> ScreenFeatures;
    public float OffsetBetweenElements = 10;

    private RectTransform RectTransform;
    private ScreenFeatureGroup PreviousGroup;

    public void Init()
    {
        PreviousGroup = PreviousTransform?.GetComponent<ScreenFeatureGroup>();
        if (PreviousGroup != null)
        {
            PreviousGroup._OnLayoutChanged += UpdateLayout;
        }

        RectTransform = GetComponent<RectTransform>();
        foreach (ScreenFeature<T> Feature in ScreenFeatures)
        {
            Feature.Init(this);
        }

        // otherwise the chain will trigger it anyway
        if (PreviousTransform == null)
        {
            UpdateLayout();
        }
    }

    public void UpdateLayout()
    {
        float OverallHeight = GetOverallHeight();
        RectTransform.sizeDelta = new Vector2(RectTransform.sizeDelta.x, OverallHeight);

        RectTransform TempPrev = GetPreviousTransform();
        RectTransform.anchoredPosition = new Vector2(
            RectTransform.anchoredPosition.x,
            TempPrev.anchoredPosition.y + TempPrev.sizeDelta.y / 2 + OverallHeight / 2 - GetMargin()
        );
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
        ScreenFeatureGroup PrevGroup = PrevTransform.GetComponent<ScreenFeatureGroup>();

        while (PrevGroup != null)
        {
            if (PrevGroup.gameObject.activeSelf)
                return CurrentValue;

            CurrentValue = bIsMargin? PrevGroup.Margin : PrevGroup.Padding;
            PrevTransform = PrevGroup.PreviousTransform;
            PrevGroup = PrevTransform.GetComponent<ScreenFeatureGroup>();
        }

        return CurrentValue;
    }

    private RectTransform GetPreviousTransform()
    {
        RectTransform PrevTransform = PreviousTransform;
        ScreenFeatureGroup PrevGroup = PrevTransform.GetComponent<ScreenFeatureGroup>();
        while (PrevGroup != null)
        {
            if (PrevGroup.gameObject.activeSelf)
                return PrevTransform;

            PrevTransform = PrevGroup.PreviousTransform;
            PrevGroup = PrevTransform.GetComponent<ScreenFeatureGroup>();
        }

        return PrevTransform;
    }

    public void ShowFeatures()
    {
        gameObject.SetActive(true);
        UpdateLayout();

        float YOffset = 0;
        float PrevHeight = 0;
        foreach (ScreenFeature<T> Feature in ScreenFeatures)
        {
            if (!Feature.ShouldBeDisplayed())
            {
                Feature.Hide();
                continue;
            }

            float CurrentHeight = Feature.GetHeight();
            YOffset = YOffset - OffsetBetweenElements - PrevHeight / 2.0f - CurrentHeight / 2.0f;
            Feature.ShowAt(YOffset);
            PrevHeight = CurrentHeight;
        }
    }

    private float GetOverallHeight()
    {
        float Height = 0;
        foreach (ScreenFeature<T> Feature in ScreenFeatures)
        {
            if (!Feature.ShouldBeDisplayed())
                continue;

            Height += Feature.GetHeight();
        }
        float PreviousPadding = PreviousGroup != null ? PreviousGroup.GetConditionalPadding() : 0;
        Height += OffsetBetweenElements * (ScreenFeatures.Count - 1) + GetPadding() + PreviousPadding;
        return Height;
    }

    public void HideFeatures()
    {
        foreach (ScreenFeature<T> Feature in ScreenFeatures)
        {
            Feature.Hide();
        }
        gameObject.SetActive(false);
    }

    public abstract T GetFeatureObject();

    public abstract bool HasFeatureObject();
}
