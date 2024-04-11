using Codice.CM.Common;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/** Helper class to make a UI screen dynamically adapt depending on which features are available to display */
public abstract class ScreenFeature<T> : MonoBehaviour
{
    public enum ScreenFeatureType
    {
        Text,
        Container
    }

    public ScreenFeatureType Type;
    protected TextMeshProUGUI TargetText;
    protected RectTransform TargetTransform;
    protected ScreenFeatureGroup<T> Target;

    public virtual void Init(ScreenFeatureGroup<T> Target)
    {
        this.Target = Target;
        TargetTransform = Type == ScreenFeatureType.Container ? GetComponent<RectTransform>() : null;
        TargetText = Type == ScreenFeatureType.Text ? GetComponent<TextMeshProUGUI>() : null;
    }

    public virtual void ShowAt(float YOffset)
    {
        TargetTransform?.gameObject.SetActive(true);
        TargetText?.gameObject.SetActive(true);
        RectTransform RectTransform = GetTransformByType();
        Vector3 Position = RectTransform.anchoredPosition;
        Position.y = YOffset;
        RectTransform.anchoredPosition = Position;
    }

    public virtual void Hide()
    {
        TargetTransform?.gameObject.SetActive(false);
        TargetText?.gameObject.SetActive(false);
    }

    public virtual float GetHeight()
    {
        RectTransform HeightTransform = GetTransformByType();
        return HeightTransform.sizeDelta.y;
    }

    protected RectTransform GetTransformByType()
    {
        return Type == ScreenFeatureType.Container ? TargetTransform : TargetText.rectTransform;
    }

    public virtual bool ShouldBeDisplayed()
    {
        return false;
    }

}
