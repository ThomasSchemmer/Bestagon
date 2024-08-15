using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/** Class to represent anything that cannot be represented by a mesh, eg resources gained */
public abstract class IconPreview : CardPreview
{
    public void InitRendering()
    {
        Visuals = CreateVisuals();
        Visuals.gameObject.name = "IconPreview";

        if (!Game.TryGetService(out PreviewSystem Previews))
            return;

        transform.SetParent(Previews.UIContainer.transform);
        Visuals.transform.SetParent(transform);
        RectTransform = Visuals.GetComponent<RectTransform>();
        MainCam = Camera.main;
    }

    public void Update()
    {
        Display();
    }

    public override void Show(HexagonVisualization Hex)
    {
        base.Show(Hex);
        HexLocation = Hex.Location;
    }

    private void Display()
    {
        if (HexLocation == null)
            return;

        Vector3 WorldPos = HexLocation.WorldLocation + Vector3.up * 8;
        Vector3 ScreenPos = MainCam.WorldToScreenPoint(WorldPos) - new Vector3(RectTransform.sizeDelta.x, RectTransform.sizeDelta.y, 0) / 2f;
        RectTransform.position = ScreenPos;
    }

    protected abstract GameObject CreateVisuals();

    protected override void SetAllowed(bool bIsAllowed)
    {
        // always allowed, do nothing
    }

    protected GameObject Visuals;
    protected RectTransform RectTransform;
    protected Camera MainCam;
    protected Location HexLocation;
}
