
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

public abstract class Card : Draggable, ISelectable
{        
    public virtual void Init(int Index) {
        CardHand = Game.GetService<CardHand>();
        ID = System.Guid.NewGuid();
        this.Index = Index;
        gameObject.layer = LayerMask.NameToLayer(Selectors.UILayerName);
        GenerateCard();
        Init();
    }

    public void GenerateCard() {
        GenerateVisuals();
        SetColor();
    }

    protected virtual void GenerateVisuals() {
        LinkTexts();
        DeleteVisuals();

        NameText.SetText(GetName());
    }

    protected virtual void DeleteVisuals()
    {
        DeleteVisuals(CostTransform);
        DeleteVisuals(UsagesTransform);
    }

    protected void DeleteVisuals(Transform Parent)
    {
        foreach (Transform Child in Parent)
        {
            Destroy(Child.gameObject);
        }
    }

    public abstract string GetName();

    public abstract bool IsPreviewable();

    public abstract bool IsCardInteractableWith(HexagonVisualization Hex);

    public abstract void InteractWith(HexagonVisualization Hex);

    public string GetSymbol()
    {
        return GetName()[..1];
    }

    protected virtual void LinkTexts() {
        CardBase = GetComponent<Image>();
        NameText = transform.Find("Name").GetComponent<TextMeshProUGUI>();
        SymbolTransform = transform.Find("Symbol").GetComponent<RectTransform>();
        CostTransform = transform.Find("Costs").GetComponent<RectTransform>();
        UsagesTransform = transform.Find("Usages").GetComponent<RectTransform>();
        EffectTransform = transform.Find("Effects").GetComponent<RectTransform>();
    }

    public void SetSelected(bool Selected) {
        if (Game.IsIn(Game.GameState.CardSelection))
            return;

        isSelected = Selected;
        Vector3 CurrentPos = transform.localPosition;
        CurrentPos.y += isSelected ? SelectOffset : 0;
        transform.localPosition = CurrentPos;
        CardHand.Sort(isHovered);
        SetColor();

        if (!Selected || !Game.TryGetService(out Selectors Selectors))
            return;

        Selectors.DeselectHexagon();
    }

    public void SetHovered(bool Hovered) {
        if (Game.IsIn(Game.GameState.CardSelection))
        {
            SetHoveredCardSelection(Hovered);
        }
        else
        {
            SetHoveredGame(Hovered);
        }
    }

    private void SetHoveredCardSelection(bool Hovered)
    {
        if (!Game.TryGetService(out CardUpgradeScreen CardScreen))
            return;

        CardScreen.ShowButtonAtCard(this, Hovered);
    }

    private void SetHoveredGame(bool Hovered)
    {
        isHovered = Hovered;
        transform.localScale = isHovered ? new Vector3(1.1f, 1.1f, 1.1f) : new Vector3(1, 1, 1);
        transform.parent.localPosition = isHovered ? CardHand.HoverPosition : CardHand.NormalPosition;
        CardHand.Sort(isHovered);
        SetColor();
    }

    public void ClickOn(Vector2 PixelPos) {}

    public void Interact() { }

    private void SetColor() {
        Color Color = isSelected ? SelectColor :
                        isHovered ? HoverColor : NormalColor;
        CardBase.color = Color;
    }

    public Transform GetUsagesTransform()
    {
        return UsagesTransform;
    }

    public Transform GetProductionTransform()
    {
        return transform.GetChild(6).GetChild(0).GetChild(1);
    }

    public bool IsEqual(ISelectable other) {
        if (!(other is Card))
            return false;

        Card OtherCard = other as Card;
        return ID == OtherCard.ID;
    }

    public bool IsSelected() {
        return isSelected;
    }

    public int GetIndex() {
        if (isHovered)
            return -1;
        if (isSelected)
            return 0;
        return Index;
    }

    public void SetIndex(int i)
    {
        transform.SetSiblingIndex(i);
        Index = i;
    }

    public void SetCanBeHovered(bool bNewState)
    {
        bCanBeHovered = bNewState;
    }

    public bool CanBeInteracted()
    {
        if (Game.Instance.bIsPaused)
            return false;

        return bCanBeHovered;
    }

    public void Use()
    {
        UseInternal();
        CardCollection Target = GetTargetAfterUse();

        CardHand.DiscardCard(this, Target);
    }

    protected abstract void UseInternal();
    protected abstract CardCollection GetTargetAfterUse();

    public abstract int GetAdjacencyRange();
    public abstract bool TryGetAdjacencyBonus(out Dictionary<HexagonConfig.HexagonType, Production> Bonus);

    public abstract bool ShouldShowAdjacency(HexagonVisualization Hex);

    public abstract bool IsCustomRuleApplying(Location NeighbourLocation);


    public bool WasUsedUpThisTurn()
    {
        return bWasUsedUp;
    }

    public virtual bool ShouldBeDeleted()
    {
        return false;
    }

    public void RefreshUsedUp()
    {
        bWasUsedUp = false;
    }

    public override void SetDragParent(RectTransform NewParent)
    {
        base.SetDragParent(NewParent);

        CardCollection NewCollection = transform.parent.GetComponent<CardCollection>();
        OldCollection.RemoveCard(this);
        NewCollection.AddCard(this);
    }

    public override void OnBeginDrag(PointerEventData eventData)
    {
        OldCollection = transform.parent.GetComponent<CardCollection>();
        base.OnBeginDrag(eventData);
    }

    public override bool CanBeDragged()
    {
        Transform Parent = transform.parent.parent;
        CardUpgradeScreen UpgradeScreen = Parent != null ? Parent.GetComponent<CardUpgradeScreen>() : null;
        bool bIsInCardSelection = Game.IsIn(Game.GameState.CardSelection);
        bool bIsCardUpgrade = UpgradeScreen != null;
        return bIsInCardSelection && !bIsCardUpgrade;
    }

    public bool CanBeLongHovered()
    {
        return true;
    }

    public string GetHoverTooltip()
    {
        return "Cards can be played on hexagons to create buildings, units or events";
    }

    public void SetHoveredAsParent(bool Hovered) {

        // would lead to deselection of card by not hovering over smaller info sections
        if (!Hovered)
            return;

        // we generally want to set this via the Selector since its different from the calling
        // selector (aka Card vs UI)
        ((ISelectable)this).GetSelectorByType().SetHovered(this, Hovered);
    }

    protected System.Guid ID;
    protected int Index;
    protected bool isHovered, isSelected;
    protected bool bCanBeHovered = false;
    protected bool bWasUsedUp = false;
    protected TextMeshProUGUI NameText;
    protected RectTransform CostTransform, SymbolTransform, UsagesTransform, EffectTransform;
    protected Image CardBase;
    protected CardHand CardHand;

    protected CardCollection OldCollection;

    public static float SelectOffset = 25f;
    public static Color NormalColor = new Color(55 / 255f, 55 / 255f, 55 / 255f);
    public static Color HoverColor = new Color(23 / 255f, 171 / 255f, 167 / 255f);
    public static Color SelectColor = new Color(23 / 255f, 95 / 255f, 171 / 255f);

}
