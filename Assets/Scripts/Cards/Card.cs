
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class Card : Draggable, Selectable
{        
    public void Init(BuildingData BuildingData, int Index) {
        this.BuildingData = BuildingData;
        CardHand = Game.GetService<CardHand>();
        ID = System.Guid.NewGuid();
        this.Index = Index;
        gameObject.layer = LayerMask.NameToLayer("Card");
        GenerateCard();
        Init();
        this.BuildingData.Init();
    }

    public void GenerateCard() {
        GenerateVisuals();
        SetColor();
    }

    private void GenerateVisuals() {
        LinkTexts();
        DeleteVisuals();

        NameText.SetText(GetName());
        //SymbolText.SetText(GetSymbol());

        if (!Game.TryGetService(out IconFactory IconFactory))
            return;

        GameObject Icons = IconFactory.GetVisualsForProduction(BuildingData.Cost);
        Icons.transform.SetParent(CostTransform, false);

        GameObject MaxWorker = IconFactory.GetVisualsForMiscalleneous(IconFactory.MiscellaneousType.Worker, BuildingData.MaxWorker);
        MaxWorker.transform.SetParent(MaxWorkerTransform, false);

        GameObject Usages = IconFactory.GetVisualsForMiscalleneous(IconFactory.MiscellaneousType.Usages, BuildingData.CurrentUsages);
        Usages.transform.SetParent(UsagesTransform, false);

        GameObject EffectObject = BuildingData.Effect.GetEffectVisuals();
        EffectObject.transform.SetParent(EffectTransform, false);

    }

    private void DeleteVisuals()
    {
        DeleteVisuals(CostTransform);
        DeleteVisuals(MaxWorkerTransform);
        DeleteVisuals(UsagesTransform);
        DeleteVisuals(EffectTransform);
    }

    private void DeleteVisuals(Transform Parent)
    {
        foreach (Transform Child in Parent)
        {
            Destroy(Child.gameObject);
        }
    }

    public string GetName()
    {
        return BuildingData.BuildingType.ToString();
    }

    public string GetSymbol()
    {
        return GetName()[..1];
    }


    private void LinkTexts() {
        CardBase = GetComponent<Image>();
        NameText = transform.Find("Name").GetComponent<TextMeshProUGUI>();
        SymbolTransform = transform.Find("Symbol").GetComponent<RectTransform>();
        CostTransform = transform.Find("Costs").GetComponent<RectTransform>();
        MaxWorkerTransform = transform.Find("MaxWorker").GetComponent<RectTransform>();
        UsagesTransform = transform.Find("Usages").GetComponent<RectTransform>();
        EffectTransform = transform.Find("Effects").GetComponent<RectTransform>();
    }

    public string GetCostText() {
        return GetBuildingData().GetCosts().GetShortDescription();
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

    public Transform GetMaxWorkerTransform()
    {
        return transform.GetChild(3);
    }

    public Transform GetUsagesTransform()
    {
        return transform.GetChild(4);
    }

    public Transform GetProductionTransform()
    {
        return transform.GetChild(6).GetChild(0).GetChild(1);
    }

    public bool IsEqual(Selectable other) {
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

    public void SetIndex(int i) {
        Index = i;
    }

    public BuildingData GetBuildingData()
    {
        return BuildingData;
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
        if (!Game.TryGetServices(out CardHand CardHand, out CardStash CardStash, out CardDeck CardDeck))
            return;

        BuildingData.CurrentUsages--;
        bool bIsUsedUp = BuildingData.CurrentUsages <= 0;
        if (bIsUsedUp)
        {
            MessageSystem.CreateMessage(Message.Type.Warning, "A card has been lost due to durability");
            bWasUsedUp = true;
        }
        CardCollection Target = bIsUsedUp ? CardStash : CardDeck;

        CardHand.DiscardCard(this, Target);
    }

    public void RefreshUsage()
    {
        BuildingData.CurrentUsages = BuildingData.MaxUsages;
    }

    public bool WasUsedUpThisTurn()
    {
        return bWasUsedUp;
    }

    public void RefreshUsedUp()
    {
        bWasUsedUp = false;
    }

    public void SetBuildingData(BuildingData BuildingData)
    {
        this.BuildingData = BuildingData;
    }

    public void GetDTOData(out BuildingData OutBuildingData)
    {
        OutBuildingData = BuildingData;
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

    protected System.Guid ID;
    protected int Index;
    protected bool isHovered, isSelected;
    protected bool bCanBeHovered = false;
    protected bool bWasUsedUp = false;
    protected TextMeshProUGUI NameText;
    protected RectTransform CostTransform, MaxWorkerTransform, SymbolTransform, EffectTransform, UsagesTransform;
    protected Image CardBase;
    protected BuildingData BuildingData;
    protected CardHand CardHand;

    protected CardCollection OldCollection;

    public static float SelectOffset = 25f;
    public static Color NormalColor = new Color(55 / 255f, 55 / 255f, 55 / 255f);
    public static Color HoverColor = new Color(23 / 255f, 171 / 255f, 167 / 255f);
    public static Color SelectColor = new Color(23 / 255f, 95 / 255f, 171 / 255f);
}
