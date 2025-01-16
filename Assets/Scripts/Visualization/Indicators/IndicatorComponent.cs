using Unity.VectorGraphics;
using UnityEngine;

/** 
 * Represents a collection of Indicators that is assigned to the target gameobject.
 * Handles creation of @IndicatorUIElements which all get information from this 
 */
public abstract class IndicatorComponent : MonoBehaviour 
{
    protected RectTransform[] Indicators;
    protected IndicatorService Service;

    private void Start()
    {
        Service = Game.GetService<IndicatorService>();
        Initialize();
        Service.Register(this);
    }

    public void OnDisable()
    {
        SetIndicatorsActive(false);
    }

    public void SetIndicatorsActive(bool bIsActive) {
        if (Indicators == null)
            return;

        foreach (var Indicator in Indicators)
        {
            if (Indicator == null)
                continue;

            Indicator.gameObject.SetActive(bIsActive);
        }
    }

    protected virtual void OnDestroy()
    {
        if (Indicators == null)
            return;

        for (int i = Indicators.Length - 1; i >= 0; i--)
        {
            if (Indicators[i] == null)
                continue;

            Destroy(Indicators[i].gameObject);
            Indicators[i] = null;
        }

        if (!Service)
            return;

        Service.Deregister(this);
    }

    /** 
     * @ShouldBeIgnored causes the hover to be swallowed, making buttons ontop not interactable anymore
     * returns "Default" layer if buttons should be clickable
     */
    protected abstract int GetTargetLayer();

    protected abstract int GetIndicatorAmount();

    protected abstract void ApplyIndicatorScreenPosition(int i, RectTransform IndicatorTransform);
    protected abstract void Initialize();
    protected abstract GameObject InstantiateIndicator(int i, RectTransform Parent);
    public abstract IndicatorService.IndicatorType GetIndicatorType();

    protected Vector3 GetIndicatorScreenOffset(int i)
    {
        int Count = GetIndicatorAmount();

        int InBetweenOffsets = (Count / 2 - 1) * OFFSET;
        int MiddleOffset = OFFSET / 2;
        MiddleOffset *= Count % 2 == 1 ? -1 : 1;
        int OverallWidth = Count / 2 * WIDTH;
        int StartX = -InBetweenOffsets + MiddleOffset - OverallWidth;
        Vector3 Pos = new Vector3();
        Pos.x = StartX + i * (WIDTH + OFFSET);

        return Pos;
    }

    protected void CreateIndicator(int i, RectTransform Parent)
    {
        GameObject Indicator = InstantiateIndicator(i, Parent);
        Indicator.layer = GetTargetLayer();
        RectTransform RectTrans = Indicator.GetComponent<RectTransform>();
        Indicators[i] = RectTrans;
        UpdateIndicatorPosition(i);
        UpdateIndicatorVisuals(i);
    }

    protected abstract void UpdateIndicatorVisuals(int i);
    public abstract bool NeedsVisualUpdate();

    private void UpdateIndicatorPosition(int i)
    {
        // not yet deleted, as marking takes a bit
        if (Indicators[i] == null)
            return;

        ApplyIndicatorScreenPosition(i, Indicators[i]);
    }

    public virtual void UpdateIndicatorVisuals()
    {
        for (int i = 0; i < Indicators.Length; i++)
        {
            UpdateIndicatorVisuals(i);
        }
    }

    public virtual void CreateVisuals()
    {
        RectTransform Container = Service.GetFor(this);
        int Count = GetIndicatorAmount();
        Indicators = new RectTransform[Count];
        for (int i = 0; i < Count; i++) {
            CreateIndicator(i, Container);
        }
    }

    public void UpdateIndicatorPositions()
    {
        for (int i = 0; i <  Indicators.Length; i++)
        {
            UpdateIndicatorPosition(i);
        }
    }

    public virtual bool IsFor(LocationSet Locations)
    {
        //overwritten in subclasses
        return false;
    }

    private static int WIDTH = 24;
    private static int OFFSET = 8;
}
