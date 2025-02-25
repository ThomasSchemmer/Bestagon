
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;
using System;
using System.Linq;
using Unity.VectorGraphics;

/**
 * Base class for any card, visualized as a playing card in the game
 * Can be selected by the player (displaying additional hover tooltips) and "played",
 * providing different effects according to type. 
 * Can be assigned to different @CardCollections 
 * Will not be saved, as its mostly visualization! See @CardDTO 
 */
public abstract class Card : Draggable, ISelectable
{        
    /** Describes whether the card GO should be visible */
    public enum Visibility : uint
    {
        Hidden = 0,
        Flipped = 1,
        Visible = 2
    }

    /** Deescribes in which collection and therefore state its in */
    public enum CardState : uint
    {
        DEFAULT = 0,
        Available = 1,
        InHand = 2,
        Played = 3,
        Disabled = 4
    }

    public virtual void Init(CardDTO DTO, int Index) {
        CardHand = Game.GetService<CardHand>();
        ID = System.Guid.NewGuid();
        transform.SetSiblingIndex(Index);
        State = DTO.State;
        bWasUsedUp = DTO.bWasUsedUp;
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
        // card selection has drag and drop
        if (Game.IsIn(Game.GameState.CardSelection))
            return;

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

    public abstract bool InteractWith(HexagonVisualization Hex);

    public abstract bool CanBeUpgraded();

    public string GetSymbol()
    {
        return GetName()[..1];
    }

    protected virtual void LinkTexts() {
        CardImage = GetComponent<SVGImage>();
        CardBorderImage = transform.Find("ImageBorder").GetComponent<SVGImage>();
        CardBorderCorruptedImage = transform.Find("ImageBorderCorrupted").GetComponent<SVGImage>();
        NameText = transform.Find("Name").GetComponent<TextMeshProUGUI>();
        SymbolTransform = transform.Find("Symbol").GetComponent<RectTransform>();
        CostTransform = transform.Find("Costs").GetComponent<RectTransform>();
        UsagesTransform = transform.Find("Usages").GetComponent<RectTransform>();
        EffectTransform = transform.Find("Effects").GetComponent<RectTransform>();
    }

    public virtual void Show(Visibility Visibility)
    {
        CardBorderImage.enabled = Visibility >= Visibility.Flipped;
        CardImage.enabled = Visibility >= Visibility.Visible;
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
        SetUsable();

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

        CardScreen.ShowButtonsAtCard(this, Hovered);
    }

    private void SetHoveredGame(bool Hovered)
    {
        isHovered = Hovered;
        transform.localScale = isHovered ? new Vector3(1.1f, 1.1f, 1.1f) : new Vector3(1, 1, 1);
        transform.parent.localPosition = isHovered ? CardHand.HoverPosition : CardHand.NormalPosition;
        CardHand.Sort(isHovered);
        SetColor();
    }

    private void SetUsable()
    {
        BuildingCard BCard = this as BuildingCard;
        if (BCard == null || Game.IsIn(Game.GameState.CardSelection))
            return;

        BuildingEntity Building = BCard.GetBuildingData();
        bool bIsAdjacent = Building.Effect.Range > 0;
        HexagonConfig.HexagonType Type = bIsAdjacent ? Building.BuildableOn : 0;
        HexMat.SetFloat("_CheckUsable", isSelected ? 1 : 0);
        HexMat.SetInt("_UsableOnMask", (int)BCard.GetBuildingData().BuildableOn);
        HexMat.SetInt("_AdjacentWithMask", (int)Type);
    }

    public void ClickOn(Vector2 PixelPos) {}

    public void Interact() { }

    private void SetColor() {
        Color Color = isSelected ? SelectColor :
                        isHovered ? HoverColor : NormalColor;
        CardBorderImage.color = Color;
    }

    public Transform GetUsagesTransform()
    {
        return UsagesTransform;
    }

    public Transform GetProductionTransform()
    {
        if (transform.childCount < 8)
            return null;

        Transform Temp = transform.GetChild(7);
        if (Temp.childCount < 1)
            return null;

        Temp = Temp.GetChild(0);
        if (Temp.childCount < 2)
            return null;
        return Temp.GetChild(1);
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

    public bool IsPinned()
    {
        return PinnedIndex > -1;
    }

    public void SetPinned(int InIndex) {
        PinnedIndex = InIndex;

        if (!Game.IsIn(Game.GameState.CardSelection))
            return;

        if (PinnedIndex < 0)
        {
            DeletePinnedIndicator();
        }
        else
        {
            CreatePinnedIndicator();
        }
    }

    private void DeletePinnedIndicator()
    {
        if (!Game.TryGetService(out IndicatorService IndicatorService))
            return;

        if (!TryGetComponent<PinnedIndicator>(out var Indicator))
            return;

        DestroyImmediate(Indicator);
    }

    private void CreatePinnedIndicator()
    {
        if (TryGetComponent<PinnedIndicator>(out var _))
            return;

        gameObject.AddComponent<PinnedIndicator>();
    }

    public int GetPinnedIndex()
    {
        return PinnedIndex;
    }

    public int GetIndex(bool bForce = false) {
        if (isHovered && !bForce)
            return -1;
        if (isSelected && !bForce)
            return 0;
        return transform.GetSiblingIndex();
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

    public CardCollection GetCurrentCollection()
    {
        return Collection;
    }

    public void SetHexMaterial(Material HexMat)
    {
        this.HexMat = HexMat;
    }

    public void Use()
    {
        UseInternal();
        CardCollection Target = GetTargetAfterUse();

        CardHand.DiscardCard(this, Target);
    }

    protected abstract void UseInternal();
    protected abstract CardCollection GetTargetAfterUse();

    //area around the actual entity from this card that is still affected
    public abstract int GetAdjacencyRange();

    // area that is representing the entity from this card directly
    public abstract LocationSet.AreaSize GetAreaSize();
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
        if (NewParent == null)
            return;

        OldCollection.RemoveCard(this);
        OldCollection.UpdatePinnedIndices();

        CardCollection NewCollection = NewParent.GetComponent<CardCollection>();
        CardGroupScreen GroupScreen = NewParent.GetComponent<CardGroupScreen>();
        TrashCan TrashCan = NewParent.GetComponent<TrashCan>();
        if (NewCollection != null)
        {
            NewCollection.SetIndex(this, Index);
            NewCollection.UpdatePinnedIndices();
        }
        else if (GroupScreen != null)
        {
            GroupScreen.StoreCard(this, Index);
        }
        else if (TrashCan != null)
        {
            ConfirmScreen.Show("This deletes the current Card and cannot be reversed. Are you sure?", OnTrashConfirm, OnTrashCancel, "Cancel", true);
        }

        if (!Game.TryGetService(out CardGroupManager Manager))
            return;

        Manager.GetActiveCardGroup().InvokeCardRemoved();
    }

    public void OnTrashConfirm()
    {
        Destroy(gameObject);
    }

    public void OnTrashCancel()
    {
        OldCollection.AddCard(this);
        OldCollection.UpdatePinnedIndices();

        if (!Game.TryGetService(out CardGroupManager Manager))
            return;

        Manager.GetActiveCardGroup().InvokeCardRemoved();
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
        return "Cards can be played on tiles to create buildings, units or events";
    }

    public CardState GetState()
    {
        return State;
    }

    public void OnAssignedToCollection(CardCollection Target)
    {
        Collection = Target;
        State = Collection.ShouldUpdateCardState() ? Collection.GetState() : State;

        // card selection has drag and drop, skip animations
        if (Game.IsIn(Game.GameState.CardSelection))
            return;
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

    protected Guid ID;
    protected bool isHovered, isSelected;
    protected bool bCanBeHovered = false;
    protected bool bWasUsedUp = false;
    protected int PinnedIndex = -1;
    protected CardState State = CardState.DEFAULT;
    protected TextMeshProUGUI NameText;
    protected RectTransform CostTransform, SymbolTransform, UsagesTransform, EffectTransform;
    protected SVGImage CardBorderImage, CardImage, CardBorderCorruptedImage;
    protected CardHand CardHand;
    protected CardCollection Collection;
    protected Material HexMat;

    protected CardCollection OldCollection;
    public List<CardMoveAnimation> Animations = new();

    public static float SelectOffset = 25f;
    public static float AnimationDurationS = 0.2f;
    public static Color NormalColor = new Color(55 / 255f, 55 / 255f, 55 / 255f);
    public static Color HoverColor = new Color(23 / 255f, 171 / 255f, 167 / 255f);
    public static Color SelectColor = new Color(23 / 255f, 95 / 255f, 171 / 255f);

    public static float Width = 200;
    public static float Height = 320;
}
