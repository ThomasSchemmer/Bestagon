using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor.Profiling.Memory.Experimental;
using System.Security.Cryptography;

public class Card : Draggable, Selectable
{        
    public static Card CreateCard(BuildingData.Type Type, int Index, Transform Parent)
    {
        GameObject CardPrefab = Resources.Load("Models/Card") as GameObject;
        GameObject GO = Instantiate(CardPrefab, Parent);
        Card Card = GO.AddComponent<Card>();

        if (!Game.TryGetService(out TileFactory BuildingFactory))
            return null;

        BuildingData BuildingData = BuildingFactory.CreateFromType(Type);
        Card.Init(BuildingData, Index);
        return Card;
    }

    public static Card CreateCardFromDTO(CardDTO DTO, int Index, Transform Parent)
    {
        GameObject CardPrefab = Resources.Load("Models/Card") as GameObject;
        GameObject GO = Instantiate(CardPrefab, Parent);
        Card Card = GO.AddComponent<Card>();

        Card.Init(DTO.BuildingData, Index);
        return Card;
    }

    public void Init(BuildingData BuildingData, int Index) {
        this.BuildingData = BuildingData;
        CardHand = Game.GetService<CardHand>();
        ID = GUID.Generate();
        this.Index = Index;
        gameObject.layer = LayerMask.NameToLayer("Card");
        GenerateCard();
        Init();
    }

    public void GenerateCard() {
        GenerateText();
        SetColor();
    }

    private void GenerateText() {
        LinkTexts();
        NameText.SetText(GetName());
        SymbolText.SetText(GetSymbol());
        CostText.SetText(GetCostText());
        EffectText.SetText(GetDescription());
        UsagesText.SetText(GetUsages());
        MaxWorkerText.SetText(GetMaxWorkers());
    }

    public string GetUsages()
    {
        return BuildingData.CurrentUsages + "/" + BuildingData.MaxUsages;
    }

    public string GetName()
    {
        return BuildingData.BuildingType.ToString();
    }

    public string GetSymbol()
    {
        return GetName()[..1];
    }

    public string GetDescription()
    {
        return BuildingData.Effect.GetDescription();
    }

    public string GetMaxWorkers()
    {
        return BuildingData.MaxWorker + "";
    }

    private void LinkTexts() {
        CardBase = GetComponent<Image>();
        NameText = transform.Find("Name").GetComponent<TextMeshProUGUI>();
        SymbolText = transform.Find("Symbol").GetComponent<TextMeshProUGUI>();
        CostText = transform.Find("Costs/Costs").GetComponent<TextMeshProUGUI>();
        EffectText = transform.Find("Effects/Effect").GetComponent<TextMeshProUGUI>();
        UsagesText = transform.Find("Usages").GetComponent<TextMeshProUGUI>();
        MaxWorkerText = transform.Find("MaxWorker").GetComponent<TextMeshProUGUI>();
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

    public void Use()
    {
        if (!Game.TryGetServices(out CardHand CardHand, out CardStash CardStash, out CardDeck CardDeck))
            return;

        BuildingData.CurrentUsages--;
        bool bIsUsedUp = BuildingData.CurrentUsages <= 0;
        if (bIsUsedUp)
        {
            MessageSystem.CreateMessage(Message.Type.Warning, "A card has been lost due to durability");
        }
        CardCollection Target = bIsUsedUp ? CardStash : CardDeck;

        CardHand.DiscardCard(this, Target);
    }

    public void RefreshUsage()
    {
        BuildingData.CurrentUsages = BuildingData.MaxUsages;
    }

    public void SetBuildingData(BuildingData BuildingData)
    {
        this.BuildingData = BuildingData;
    }

    public void GetDTOData(out GUID OutID, out BuildingData OutBuildingData)
    {
        OutID = ID;
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

    protected GUID ID;
    protected int Index;
    protected bool isHovered, isSelected;
    protected TextMeshProUGUI NameText, SymbolText, CostText, EffectText, UsagesText, MaxWorkerText;
    protected Image CardBase;
    protected BuildingData BuildingData;
    protected CardHand CardHand;

    protected CardCollection OldCollection;

    public static float SelectOffset = 25f;
    public static Color NormalColor = new Color(55 / 255f, 55 / 255f, 55 / 255f);
    public static Color HoverColor = new Color(23 / 255f, 171 / 255f, 167 / 255f);
    public static Color SelectColor = new Color(23 / 255f, 95 / 255f, 171 / 255f);
}
