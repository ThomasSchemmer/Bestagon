using Unity.VectorGraphics;
using UnityEngine;

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

    protected virtual void OnDestroy()
    {
        if (!Service)
            return;

        for (int i = Indicators.Length - 1; i >= 0; i--)
        {
            if (Indicators[i] == null)
                continue;

            Destroy(Indicators[i].gameObject);
            Indicators[i] = null;
        }

        Service.Deregister(this);
    }

    protected abstract int GetIndicatorAmount();

    protected abstract void ApplyIndicatorScreenPosition(int i, RectTransform IndicatorTransform);
    protected abstract Sprite GetIndicatorSprite(int i);
    protected abstract void Initialize();

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
        GameObject Indicator = Instantiate(Service.IndicatorPrefab);
        RectTransform RectTrans = Indicator.GetComponent<RectTransform>();
        RectTrans.SetParent(Parent);
        Indicators[i] = RectTrans;
        UpdateSpritePosition(i);
        UpdateSpriteVisuals(i);
    }

    protected void UpdateSpriteVisuals(int i)
    {
        if (Indicators[i] == null)
            return;

        SVGImage Image = Indicators[i].GetComponent<SVGImage>();
        Image.sprite = GetIndicatorSprite(i);
    }

    protected void UpdateSpritePosition(int i)
    {
        // not yet deleted, as marking takes a bit
        if (Indicators[i] == null)
            return;

        ApplyIndicatorScreenPosition(i, Indicators[i]);
    }

    public void UpdateSpritesVisuals()
    {
        for (int i = 0; i < Indicators.Length; i++)
        {
            UpdateSpriteVisuals(i);
        }
    }

    public void CreateVisuals()
    {
        RectTransform Container = Service.GetFor(this);
        int Count = GetIndicatorAmount();
        Indicators = new RectTransform[Count];
        for (int i = 0; i < Count; i++) {
            CreateIndicator(i, Container);
        }
    }

    public void UpdateVisuals()
    {
        for (int i = 0; i <  Indicators.Length; i++)
        {
            UpdateSpritePosition(i);
        }
    }

    public virtual bool IsFor(Location Location)
    {
        //overwritten in subclasses
        return false;
    }

    private static int WIDTH = 24;
    private static int OFFSET = 8;
}
