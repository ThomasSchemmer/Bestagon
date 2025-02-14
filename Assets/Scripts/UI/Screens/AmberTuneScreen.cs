using System.Collections;
using System.Collections.Generic;
using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class AmberTuneScreen : MonoBehaviour
{
    public AttributeType Type;
    public List<SVGImage> Icons = new();
    public List<Button> ControlButtons = new();
    public GameObject AddButton, SubtractButton;
    public TMPro.TextMeshProUGUI FlavourText;

    public void Initialize(AttributeType Type, int Index)
    {
        this.Type = Type;
        if (!Game.TryGetService(out AmberService Ambers))
            return;

        int CurrentCount = Ambers.Infos[Type].CurrentValue;
        int MaxCount = Ambers.Infos[Type].MaxValue;
        var SelfRect = GetComponent<RectTransform>();
        SelfRect.sizeDelta = new Vector2(Offset.x * MaxCount - OffsetX, Height);
        SelfRect.anchoredPosition = new Vector2(SelfRect.anchoredPosition.x, -Height * Index + InitialYOffset);
        Icons = new(MaxCount);

        for (int i = 0; i < MaxCount; i++)
        {
            CreateIcon(i, CurrentCount, IconFactory.MiscellaneousType.Amber, false);
        }
        
        SubtractButton = CreateIcon(-2, CurrentCount, IconFactory.MiscellaneousType.Subtract, true);
        AddButton = CreateIcon(MaxCount + 1, CurrentCount, IconFactory.MiscellaneousType.Add, true);
        MakeButton(SubtractButton, Subtract);
        MakeButton(AddButton, Add);
        MakeFlavourText();
        Refresh();
    }

    private void MakeFlavourText()
    {
        var SelfRect = GetComponent<RectTransform>();
        GameObject TextGO = new("Flavour");
        RectTransform Rect = TextGO.AddComponent<RectTransform>();
        Rect.anchoredPosition = new (0, TextYOffset);
        Rect.sizeDelta = new Vector2(500, 25);
        Rect.SetParent(SelfRect, false);
        FlavourText = TextGO.AddComponent<TMPro.TextMeshProUGUI>();
        FlavourText.alignment = TMPro.TextAlignmentOptions.Center;
        FlavourText.fontSize = 22;
    }

    private void MakeButton(GameObject Obj, UnityAction Action)
    {
        Button Button = Obj.AddComponent<Button>();
        Button.onClick.AddListener(Action);
        ControlButtons.Add(Button);
    }

    private void Subtract()
    {
        if (!Game.TryGetService(out AmberService Ambers))
            return;

        Ambers.Decrease(Type);
    }

    private void Add()
    {
        if (!Game.TryGetService(out AmberService Ambers))
            return;

        Ambers.Increase(Type);
    }

    public void Refresh()
    {
        if (!Game.TryGetService(out AmberService Ambers))
            return;

        int CurrentCount = Ambers.Infos[Type].CurrentValue;
        for (int i = 0; i < Icons.Count; i++)
        {
            bool bIsActive = i < CurrentCount;
            SetColor(Icons[i], bIsActive);
        }
        FlavourText.text = GetFlavourText();

        bool bIsInCardSelection = Game.IsIn(Game.GameState.CardSelection);
        for (int i = 0; i < ControlButtons.Count; i++)
        {
            ControlButtons[i].interactable = CanInteract(i == 0);
            ControlButtons[i].gameObject.SetActive(bIsInCardSelection);
        }
    }

    private bool CanInteract(bool bIsMinus)
    {
        if (!Game.TryGetService(out AmberService Amber))
            return false;

        if (bIsMinus)
            return Amber.CanDecrease(Type);

        return Amber.CanIncrease(Type);
    }



    private string GetFlavourText()
    {
        if (!Game.TryGetService(out AmberService Amber))
            return string.Empty;

        int Multiplier = Amber.Infos[Type].CurrentValue;
        return Amber.AvailableAmbers[Type].Modifiers[0].GetDescription(Multiplier);
    }

    private GameObject CreateIcon(int Index, int CurrentCount, IconFactory.MiscellaneousType Type, bool bIsControl)
    {
        if (!Game.TryGetService(out IconFactory IconFactory))
            return null;

        bool bIsActive = Index < CurrentCount;
        var IconGO = new GameObject("Icon");
        var Rect = IconGO.AddComponent<RectTransform>();
        Rect.transform.SetParent(transform, false);
        Rect.sizeDelta = bIsControl ? ControlSize : Size; 
        Rect.anchoredPosition = Index * Offset + Origin;
        Rect.anchorMin = new(0, Rect.anchorMin.y);
        Rect.anchorMax = new(0, Rect.anchorMax.y);
        var SVG = IconGO.AddComponent<SVGImage>();
        var Icon = IconFactory.GetIconForMisc(Type);
        SVG.sprite = Icon;
        SetColor(SVG, bIsActive || bIsControl);
        if (!bIsControl)
        {
            Icons.Add(SVG);
        }
        return IconGO;
    }

    private void SetColor(SVGImage SVG, bool bIsActive)
    {
        SVG.color = !bIsActive ? Color.gray : Color.white;
    }

    public static float OffsetX = 15;
    public static Vector2 ControlSize = new(40, 40);
    public static Vector2 Size = new(50, 75);
    public static Vector2 Origin = new(Size.x/2, 0);
    public static Vector2 Offset = new(Size.x + OffsetX, 0);
    public static float Height = 150;
    public static float TextYOffset = - 60;
    public static float InitialYOffset = 75;
}
