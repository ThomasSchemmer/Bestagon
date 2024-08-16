
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;
using System;
using System.Linq;

public abstract class Card : Draggable, ISelectable
{        
    public enum Visibility : uint
    {
        Hidden = 0,
        Flipped = 1,
        Visible = 2
    }

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

    public void Update()
    {
        int Index = 0;
        float RemainingTime = Time.deltaTime;
        while (Index < Animations.Count && RemainingTime > 0)
        {
            CardMoveAnimation Animation = Animations[Index];

            Visibility TargetVisibility = Animation.TargetCollection.ShouldCardsBeDisplayed() ? Visibility.Visible : Visibility.Flipped;
            Show(TargetVisibility);

            float Diff = Mathf.Min(RemainingTime, Animation.RemainingDurationS);
            Animation.RemainingDurationS -= Diff;
            RemainingTime -= Diff;

            float t = 1 - Animation.RemainingDurationS / AnimationDurationS;
            float ct = Mathf.Clamp(t, 0, 1);

            Vector3 StartPosition = Animation.StartPosition;
            Vector3 TargetPosition = Animation.TargetCollection.transform.position;
            transform.position = Vector3.Lerp(StartPosition, TargetPosition, ct);

            float SourceSize = Animation.SourceCollection.GetCardSize();
            float TargetSize = Animation.TargetCollection.GetCardSize();
            float ct1 = Mathf.Clamp(ct, 0, 0.5f) * 2;
            float ct2 = Mathf.Clamp(ct - 0.5f, 0, 0.5f) * 2;

            Vector3 SourceScale = Mathf.Lerp(SourceSize, 0.5f, ct1) * Vector3.one;
            Vector3 TargetScale = Mathf.Lerp(0.5f, TargetSize, ct2) * Vector3.one;
            transform.localScale = ct < 0.5f ? SourceScale : TargetScale;

            if (Mathf.Approximately(Animation.RemainingDurationS, 0))
            {
                Animations.RemoveAt(0);
                TargetVisibility = Animation.TargetCollection.ShouldCardsBeDisplayed() ? Visibility.Visible : Visibility.Hidden;
                Show(TargetVisibility);
                Animation.TargetCollection.Sort();
            }
            else
            {
                Index++;
            }
        }
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

    public abstract bool CanBeUpgraded();

    public string GetSymbol()
    {
        return GetName()[..1];
    }

    protected virtual void LinkTexts() {
        CardBaseImage = GetComponent<Image>();
        CardImage = transform.Find("Image").GetComponent<Image>();
        NameText = transform.Find("Name").GetComponent<TextMeshProUGUI>();
        SymbolTransform = transform.Find("Symbol").GetComponent<RectTransform>();
        CostTransform = transform.Find("Costs").GetComponent<RectTransform>();
        UsagesTransform = transform.Find("Usages").GetComponent<RectTransform>();
        EffectTransform = transform.Find("Effects").GetComponent<RectTransform>();
    }

    public virtual void Show(Visibility Visibility)
    {
        CardBaseImage.enabled = Visibility >= Visibility.Flipped;
        CardImage.gameObject.SetActive(Visibility >= Visibility.Visible);
        NameText.gameObject.SetActive(Visibility >= Visibility.Visible);
        SymbolTransform.gameObject.SetActive(Visibility >= Visibility.Visible);
        CostTransform.gameObject.SetActive(Visibility >= Visibility.Visible);
        UsagesTransform.gameObject.SetActive(Visibility >= Visibility.Visible);
        EffectTransform.gameObject.SetActive(Visibility >= Visibility.Visible);
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
        if (!CanBeUpgraded())
        {
            Hovered = false;
        }

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
        CardBaseImage.color = Color;
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

    public override void SetIndex(int i)
    {
        base.SetIndex(i);
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

    public override void SetDragParent(RectTransform NewParent, int Index)
    {
        base.SetDragParent(NewParent, Index);

        CardCollection NewCollection = transform.parent.GetComponent<CardCollection>();
        OldCollection.RemoveCard(this);
        NewCollection.SetIndex(this, Index);
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

    public void OnAssignedToCollection(CardCollection Target)
    {
        if (Animations.Count == 0)
            return;

        CardMoveAnimation Animation = Animations.Last();
        Animation.TargetCollection = Target;
        Animation.RemainingDurationS = AnimationDurationS;
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
    protected Image CardBaseImage, CardImage;
    protected CardHand CardHand;

    protected CardCollection OldCollection;
    public List<CardMoveAnimation> Animations = new();

    public static float SelectOffset = 25f;
    public static float AnimationDurationS = 0.2f;
    public static Color NormalColor = new Color(55 / 255f, 55 / 255f, 55 / 255f);
    public static Color HoverColor = new Color(23 / 255f, 171 / 255f, 167 / 255f);
    public static Color SelectColor = new Color(23 / 255f, 95 / 255f, 171 / 255f);

}
