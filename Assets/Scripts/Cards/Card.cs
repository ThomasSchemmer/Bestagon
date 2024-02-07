using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class Card : Draggable, Selectable
{        
    public static Card CreateCard(BuildingData.Type Type, int Index, Transform Parent)
    {
        GameObject CardPrefab = Resources.Load("/Models/Card") as GameObject;
        GameObject GO = Instantiate(CardPrefab, Parent);
        Card Card = GO.AddComponent<Card>();

        if (!Game.TryGetService(out BuildingFactory BuildingFactory))
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

    private void GenerateCard() {
        GenerateText();
        SetColor();
    }

    private void GenerateText() {
        LinkTexts();
        NameText.SetText(GetName());
        SymbolText.SetText(GetSymbol());
        CostText.SetText(GetCostText());
        EffectText.SetText(GetDescription());
    }

    private string GetName()
    {
        return BuildingData.BuildingType.ToString();
    }

    private string GetSymbol()
    {
        return GetName()[..1];
    }

    private string GetDescription()
    {
        return BuildingData.Effect.GetDescription();
    }

    private void LinkTexts() {
        CardBase = GetComponent<Image>();
        NameText = transform.Find("Name").GetComponent<TextMeshProUGUI>();
        SymbolText = transform.Find("Symbol").GetComponent<TextMeshProUGUI>();
        CostText = transform.Find("Costs/Costs").GetComponent<TextMeshProUGUI>();
        EffectText = transform.Find("Effects/Effect").GetComponent<TextMeshProUGUI>();
    }

    private string GetCostText() {
        return GetBuildingData().GetCosts().GetShortDescription();
    }

    public void SetSelected(bool Selected) {
        isSelected = Selected;
        Vector3 CurrentPos = transform.localPosition;
        CurrentPos.y += isSelected ? SelectOffset : 0;
        transform.localPosition = CurrentPos;
        CardHand.Sort(isHovered);
        SetColor();
    }

    public void SetHovered(bool Hovered) {
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

    

    public void GetDTOData(out GUID OutID, out BuildingData OutBuildingData)
    {
        OutID = ID;
        OutBuildingData = BuildingData;
    }

    protected GUID ID;
    protected int Index;
    protected bool isHovered, isSelected;
    protected TextMeshProUGUI NameText, SymbolText, CostText, EffectText;
    protected Image CardBase;
    protected BuildingData BuildingData;
    protected CardHand CardHand;

    public static float SelectOffset = 25f;
    public static Color NormalColor = new Color(55 / 255f, 55 / 255f, 55 / 255f);
    public static Color HoverColor = new Color(23 / 255f, 171 / 255f, 167 / 255f);
    public static Color SelectColor = new Color(23 / 255f, 95 / 255f, 171 / 255f);
}
