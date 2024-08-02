using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StockpileGroupScreen : MonoBehaviour, UIElement
{
    public Color StandardColor, HoverColor, SelectionColor;

    protected Image HighlightImage;
    protected Transform Container;
    protected bool bIsHovered = false;
    protected StockpileItemScreen GroupItemScreen;
    protected StockpileItemScreen[] ItemScreens;
    protected bool bDisplayActiveOnly = false;

    protected static StockpileGroupScreen SelectedInstance;
    protected StockpileScreen Parent;
    protected int ProductionGroupIndex;

    public static int WIDTH = 85;
    public static int OFFSET = 12;
    public static int INITIAL_OFFSET = 10;

    public void Initialize(StockpileScreen Parent, int Index, GameObject ItemPrefab, Stockpile Stockpile, IconFactory IconFactory, bool bDisplayActiveOnly)
    {
        HighlightImage = transform.GetChild(0).GetComponent<Image>();
        HighlightImage.color = StandardColor;
        Container = transform.GetChild(2);

        this.bDisplayActiveOnly = bDisplayActiveOnly;
        this.Parent = Parent;
        this.ProductionGroupIndex = Index;
        InitializeHeader(Index, Stockpile, IconFactory);

        InitializeItems(Index, ItemPrefab, Stockpile, IconFactory);
        Container.gameObject.SetActive(!bDisplayActiveOnly);

        UpdateVisuals();
    }

    private void InitializeHeader(int Index, Stockpile Stockpile, IconFactory IconFactory)
    {
        if (Parent.ShouldHeaderBeIcon())
        {
            GroupItemScreen = transform.GetChild(1).GetComponent<StockpileItemScreen>();
            GroupItemScreen.Initialize(this, Index, Stockpile, IconFactory);
            GroupItemScreen.UpdateVisuals();
            GroupItemScreen.SetSelectionEnabled(false);
        }
        else
        {
            DestroyImmediate(transform.GetChild(0).GetComponent<Image>());
            DestroyImmediate(transform.GetChild(1).gameObject);
            TMPro.TextMeshProUGUI Text = transform.GetChild(0).gameObject.AddComponent<TMPro.TextMeshProUGUI>();
            Text.text = ((Production.GoodsType)Production.Indices[Index]).ToString();
            Text.enableAutoSizing = true;
            RectTransform Rect = Text.gameObject.GetComponent<RectTransform>();
            Rect.anchoredPosition = new(Parent.GetContainerOffset().x, 0);
            Rect.sizeDelta = new(Parent.GetContainerSize().x - 15, Rect.sizeDelta.y);
        }
    }

    protected virtual void InitializeItems(int GroupIndex, GameObject ItemPrefab, Stockpile Stockpile, IconFactory IconFactory)
    {
        int MinIndex = Production.Indices[GroupIndex];
        int MaxIndex = Production.Indices[GroupIndex + 1];

        // adapt container size
        int Count = Mathf.RoundToInt((MaxIndex - MinIndex + 1) / 2f);
        float ContainerHeight = Count * Parent.GetElementTotalSize().y;
        RectTransform ContainerRect = Container.GetComponent<RectTransform>();
        ContainerRect.sizeDelta = new Vector2(Parent.GetContainerSize().x, ContainerHeight);
        ContainerRect.anchoredPosition = new Vector2(
            Parent.GetContainerOffset().x,
            -Parent.GetContainerOffset().y - Count * Parent.GetElementTotalSize().y / 2
        );

        float ContainerStart = ContainerHeight / 2 - Parent.GetElementTotalSize().y / 2;

        ItemScreens = new StockpileItemScreen[MaxIndex - MinIndex];
        for (int Index = MinIndex; Index < MaxIndex; Index++)
        {
            int IndexInGroup = Index - MinIndex;
            float Offset = Parent.GetContainerSize().x / 4;
            float x = (IndexInGroup % 2) == 0 ? -Offset : Offset;
            float y = ContainerStart - Parent.GetElementTotalSize().y * (IndexInGroup / 2);
            GameObject NewItem = Instantiate(ItemPrefab);
            NewItem.transform.SetParent(Container);
            NewItem.transform.localPosition = new Vector3(x, y, 0);

            StockpileItemScreen ItemScreen = NewItem.GetComponent<StockpileItemScreen>();
            ItemScreen.Initialize(null, Index, Stockpile, IconFactory);
            ItemScreens[IndexInGroup] = ItemScreen;
            Parent.AdaptItemScreen(this, ItemScreen);
        }
    }


    public void UpdateIndicatorCount()
    {
        if (GroupItemScreen != null)
        {
            GroupItemScreen.UpdateIndicatorCount();
        }
        
        foreach (StockpileItemScreen ItemScreen in ItemScreens)
        {
            ItemScreen.UpdateIndicatorCount();
        }
    }

    public void UpdateVisuals()
    {
        if (GroupItemScreen != null)
        {
            GroupItemScreen.UpdateVisuals();
        }
        foreach (StockpileItemScreen ItemScreen in ItemScreens)
        {
            ItemScreen.UpdateVisuals();
        }
    }

    public void ClickOn(Vector2 PixelPos)
    {
        if (!bDisplayActiveOnly)
            return;

        if (SelectedInstance == this)
        {
            ShowContainer(false);
            SelectedInstance = null;
        }
        else
        {
            if (SelectedInstance != null)
            {
                SelectedInstance.ShowContainer(false);
            }
            SelectedInstance = this;
            SelectedInstance.ShowContainer(true);
        }
    }
    
    public Transform GetContainer()
    {
        return Container;
    }
    
    public void Interact() {}

    public bool IsEqual(ISelectable Other)
    {
        if (Other is not StockpileGroupScreen)
            return false;

        StockpileGroupScreen OtherSGS = Other as StockpileGroupScreen;
        return ProductionGroupIndex == OtherSGS.ProductionGroupIndex;
    }

    public void ShowContainer(bool bIsVisible)
    {
        if (!bDisplayActiveOnly)
            return;

        HighlightImage.color = bIsVisible ? SelectionColor : StandardColor;
        Container.gameObject.SetActive(bIsVisible);
    }

    public void Show(bool bIsVisible)
    {
        gameObject.SetActive(bIsVisible);
    }

    private bool IsSelected()
    {
        return SelectedInstance == this;
    }

    public void SetHovered(bool Hovered)
    {
        if (!bDisplayActiveOnly)
            return;

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
        if (GroupItemScreen != null)
        {
            return GroupItemScreen.GetHoverTooltip();
        }
        return "";
    }

    public float GetContainerHeight()
    {
        RectTransform ContainerRect = Container.GetComponent<RectTransform>();
        return ContainerRect.sizeDelta.y;
    }

    public static StockpileGroupScreen GetSelectedInstance()
    {
        return SelectedInstance;
    }
}
