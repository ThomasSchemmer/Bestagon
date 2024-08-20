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

    private TMPro.TextMeshProUGUI IndicatorText;
    private NumberedIconScreen IconScreen;
    private Stockpile Stockpile;
    private Button ItemButton;

    public void Initialize(StockpileGroupScreen Screen, int Index, Stockpile Stockpile, IconFactory IconFactory)
    {
        this.Stockpile = Stockpile;
        ParentScreen = Screen;
        ProductionIndex = Index;
        IndicatorText = transform.GetChild(0).GetComponent<TMPro.TextMeshProUGUI>();
        IconScreen = transform.GetChild(1).GetComponent<NumberedIconScreen>();
        Production.Type? Type = ParentScreen != null ? null : (Production.Type)ProductionIndex;
        Production.GoodsType? GoodsType = ParentScreen != null ? (Production.GoodsType)ProductionIndex : null;
        Sprite Sprite = Type == null ? 
            IconFactory.GetIconForProductionType((Production.GoodsType)GoodsType) :
            IconFactory.GetIconForProduction((Production.Type)Type);
        IconScreen.Initialize(Sprite, GetHoverTooltip(), this);
        IconScreen.SetAmountAlignment(TMPro.TextAlignmentOptions.Midline);
    }

    public void UpdateVisuals()
    {
        UpdateIndicatorCount();
        IconScreen.UpdateVisuals(GetCount(false));
    }

    public void SetSelectionEnabled(bool bEnabled)
    {
        IconScreen.SetSelectionEnabled(bEnabled);
        IndicatorText.gameObject.layer = bEnabled ? LayerMask.NameToLayer(Selectors.UILayerName) : 0;
    }

    public void UpdateIndicatorCount()
    {
        int Count = GetCount(true);

        string Text = (Count > 0 ? "+" : "") + Count;
        IndicatorText.text = Count == 0 ? string.Empty : Text;
        IndicatorText.color = Count >= 0 ? OKColor : NegativeColor;
    }

    public void SetItemSubscription(Production.Type Type, int Amount)
    {
        IconScreen.SetSubscription(Type, Amount);
    }

    private int GetCount(bool bIsSimulated)
    {
        int Count = ParentScreen != null ?
            Stockpile.GetResourceGroupCount(ProductionIndex, bIsSimulated) :
            Stockpile.GetResourceCount(ProductionIndex, bIsSimulated);
        return Count;
    }

    public void ClickOn(Vector2 PixelPos) {
        if (ParentScreen == null)
            return;

        ParentScreen.ClickOn(PixelPos);
    }

    public void Interact() {}

    public bool IsEqual(ISelectable other)
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

    public bool CanBeLongHovered()
    {
        return true;
    }

    public string GetHoverTooltip()
    {
        if (ParentScreen != null)
            return ((Production.GoodsType)Production.Indices[ProductionIndex]).ToString();

        return ((Production.Type)ProductionIndex).ToString();
    }

    public Button GetItemButton(IconFactory IconFactory, Transform Parent)
    {
        if (ItemButton != null)
            return ItemButton;

        GameObject ButtonObject = IconFactory.ConvertVisualsToButton(Parent, GetComponent<RectTransform>());
        ItemButton = ButtonObject.GetComponent<Button>();
        return ItemButton;
    }

    private static float OKValue = 0.132f;
    private static Color OKColor = new(OKValue, OKValue, OKValue);
    private static Color NegativeColor = Color.red;
}
