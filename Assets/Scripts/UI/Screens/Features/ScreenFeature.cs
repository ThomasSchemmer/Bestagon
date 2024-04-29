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
    protected bool bIsInit = false;

    public virtual void Init(ScreenFeatureGroup<T> Target)
    {
        this.Target = Target;
        TargetTransform = Type == ScreenFeatureType.Container ? GetComponent<RectTransform>() : null;
        TargetText = Type == ScreenFeatureType.Text ? GetComponent<TextMeshProUGUI>() : null;
        bIsInit = true;
    }

    public virtual void ShowAt(float YOffset)
    {
        if (!bIsInit)
            return;

        TargetTransform?.gameObject.SetActive(true);
        TargetText?.gameObject.SetActive(true);
        RectTransform RectTransform = GetTransformByType();
        Vector3 Position = RectTransform.anchoredPosition;
        Position.y = YOffset;
        RectTransform.anchoredPosition = Position;
    }

    public virtual void Hide()
    {
        if (!bIsInit)
            return;

        TargetTransform?.gameObject.SetActive(false);
        TargetText?.gameObject.SetActive(false);
    }

    public virtual float GetHeight()
    {
        RectTransform HeightTransform = GetTransformByType();
        return HeightTransform.sizeDelta.y;
    }

    public void SetConditionalPadding(float Padding)
    {
        Target.SetConditionalPadding(Padding);
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
