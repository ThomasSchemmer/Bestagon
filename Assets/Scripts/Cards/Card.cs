using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public abstract class Card : MonoBehaviour, Selectable
{
    public abstract string GetName();
    public abstract string GetDescription();
    public abstract string GetSymbol();

    public abstract BuildingData GetBuildingData();
        
    public static T CreateCard<T>(int Index, GameObject CardPrefab, Transform Parent) where T : Card{
        GameObject GO = Canvas.Instantiate(CardPrefab, Parent);
        T Card = GO.AddComponent<T>();
        Card.Init(Index);
        return Card;
    }

    public void Init(int Index) {
        id = GUID.Generate();
        this.Index = Index;
        gameObject.layer = LayerMask.NameToLayer("Card");
        GenerateCard();
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
        return id == OtherCard.id;
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

    protected GUID id;
    protected int Index;
    protected bool isHovered, isSelected;
    protected TextMeshProUGUI NameText, SymbolText, CostText, EffectText;
    protected Image CardBase;

    public static float SelectOffset = 25f;
    public static Color NormalColor = new Color(55 / 255f, 55 / 255f, 55 / 255f);
    public static Color HoverColor = new Color(23 / 255f, 171 / 255f, 167 / 255f);
    public static Color SelectColor = new Color(23 / 255f, 95 / 255f, 171 / 255f);
}
