using System.Collections;
using System.Collections.Generic;
using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.UI;

public class StockpileItemScreen : MonoBehaviour, UIElement
{    
    private StockpileGroupScreen ParentScreen;
    // either represents a group index (for coresponding ParentScreen), or an actual resource type index
    private int ProductionIndex = -1;

    private SVGImage IndicatorRenderer;
    private NumberedIconScreen ProductionUnitScreen;
    private int[] PastCounts = new int[3];
    private Stockpile Stockpile;
    private IconFactory IconFactory;

    public void Initialize(StockpileGroupScreen Screen, int Index, Stockpile Stockpile, IconFactory IconFactory)
    {
        this.IconFactory = IconFactory;
        this.Stockpile = Stockpile;
        ParentScreen = Screen;
        ProductionIndex = Index;
        IndicatorRenderer = transform.GetChild(0).GetComponent<SVGImage>();
        ProductionUnitScreen = transform.GetChild(1).GetComponent<NumberedIconScreen>();
        Production.Type? Type = ParentScreen != null ? null : (Production.Type)ProductionIndex;
        Sprite Sprite = Type == null ? null : IconFactory.GetIconForProduction((Production.Type)Type);
        ProductionUnitScreen.Initialize(Sprite, true);

        int Count = GetCount();
        PastCounts[0] = Count;
        PastCounts[1] = Count;
        PastCounts[2] = Count;
    }

    public void UpdateVisuals()
    {
        int CountDifference = PastCounts[2] - PastCounts[0];
        IconFactory.MiscellaneousType Trend = CountDifference > 0 ? IconFactory.MiscellaneousType.TrendUp : IconFactory.MiscellaneousType.TrendDown;
        IndicatorRenderer.sprite = IconFactory.GetIconForMisc(Trend);
        IndicatorRenderer.enabled = CountDifference != 0;

        ProductionUnitScreen.UpdateVisuals(GetCount());
    }

    public  void UpdateIndicatorCount()
    {
        PastCounts[0] = PastCounts[1];
        PastCounts[1] = PastCounts[2];
        PastCounts[2] = GetCount();
    }

    private int GetCount()
    {
        int Count = ParentScreen != null ?
            Stockpile.GetResourceGroupCount(ProductionIndex) :
            Stockpile.GetResourceCount(ProductionIndex);
        return Count;
    }

    public void ClickOn(Vector2 PixelPos) {
        if (ParentScreen == null)
            return;

        ParentScreen.ClickOn(PixelPos);
    }

    public void Interact() {}

    public bool IsEqual(Selectable other)
    {
        if (other is not StockpileItemScreen)
            return false;

        return ProductionIndex == ((StockpileItemScreen)other).ProductionIndex;
    }

    public void SetHovered(bool Hovered)
    {
        if (ParentScreen == null)
            return;

        ParentScreen.SetHovered(Hovered);
    }

    public void SetSelected(bool Selected)
    {
        if (ParentScreen == null)
            return;

        ParentScreen.SetSelected(Selected);
    }

    public int GetProductionIndex()
    {
        return ProductionIndex;
    }
}
