using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StockpileGroupScreen : MonoBehaviour, UIElement
{
    public Color StandardColor, HoverColor, SelectionColor;

    private Image HighlightImage;
    private Transform Container;
    private bool bIsHovered = false;
    private StockpileItemScreen GroupItemScreen;
    private StockpileItemScreen[] ItemScreens;

    private static StockpileGroupScreen SelectedInstance;

    public static int WIDTH = 76;
    public static int OFFSET = 14;

    public void Initialize(int Index, GameObject ItemPrefab, Stockpile Stockpile, IconFactory IconFactory)
    {
        HighlightImage = transform.GetChild(0).GetComponent<Image>();
        HighlightImage.color = StandardColor;
        Container = transform.GetChild(2);

        GroupItemScreen = transform.GetChild(1).GetComponent<StockpileItemScreen>();
        GroupItemScreen.Initialize(this, Index, Stockpile, IconFactory);
        GroupItemScreen.UpdateVisuals();
        GroupItemScreen.SetSelectionEnabled(false);

        InitializeItems(Index, ItemPrefab, Stockpile, IconFactory);
        Container.gameObject.SetActive(false);

        UpdateVisuals();
    }

    private void InitializeItems(int GroupIndex, GameObject ItemPrefab, Stockpile Stockpile, IconFactory IconFactory)
    {
        int MinIndex = Production.Indices[GroupIndex];
        int MaxIndex = Production.Indices[GroupIndex + 1];
        ItemScreens = new StockpileItemScreen[MaxIndex - MinIndex];
        for (int Index = MinIndex; Index < MaxIndex; Index++)
        {
            int IndexInGroup = Index - MinIndex;
            int x = (IndexInGroup % 2) == 0 ? -42 : 42;
            int y = 65 - 40 * (IndexInGroup / 2);
            GameObject NewItem = Instantiate(ItemPrefab);
            NewItem.transform.SetParent(Container);
            NewItem.transform.localPosition = new Vector3(x, y, 0);

            StockpileItemScreen ItemScreen = NewItem.GetComponent<StockpileItemScreen>();
            ItemScreen.Initialize(null, Index, Stockpile, IconFactory);
            ItemScreens[IndexInGroup] = ItemScreen;
        }
    }

    public void UpdateIndicatorCount()
    {
        GroupItemScreen.UpdateIndicatorCount();
        foreach (StockpileItemScreen ItemScreen in ItemScreens)
        {
            ItemScreen.UpdateIndicatorCount();
        }
    }

    public void UpdateVisuals()
    {
        GroupItemScreen.UpdateVisuals();
        foreach (StockpileItemScreen ItemScreen in ItemScreens)
        {
            ItemScreen.UpdateVisuals();
        }
    }

    public void ClickOn(Vector2 PixelPos)
    {
        if (SelectedInstance == this)
        {
            Show(false);
            SelectedInstance = null;
        }
        else
        {
            if (SelectedInstance != null)
            {
                SelectedInstance.Show(false);
            }
            SelectedInstance = this;
            SelectedInstance.Show(true);
        }
    }

    public void Interact() {}

    public bool IsEqual(ISelectable Other)
    {
        if (Other is not StockpileGroupScreen)
            return false;

        StockpileGroupScreen OtherSGS = Other as StockpileGroupScreen;
        return GroupItemScreen.GetProductionIndex() == OtherSGS.GroupItemScreen.GetProductionIndex();
    }

    public void Show(bool bIsVisible)
    {
        HighlightImage.color = bIsVisible ? SelectionColor : StandardColor;
        Container.gameObject.SetActive(bIsVisible);
    }

    private bool IsSelected()
    {
        return SelectedInstance == this;
    }

    public void SetHovered(bool Hovered)
    {
        bIsHovered = Hovered;
        HighlightImage.color = IsSelected() ? SelectionColor :
                        bIsHovered ? HoverColor : StandardColor;
    }


    public bool CanBeLongHovered()
    {
        return true;
    }

    public void SetSelected(bool Selected) {}

    public string GetHoverTooltip()
    {
        return GroupItemScreen.GetHoverTooltip();
    }
}
