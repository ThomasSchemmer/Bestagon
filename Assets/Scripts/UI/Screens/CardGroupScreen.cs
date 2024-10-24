using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/**
 * Represents the UI for a @CardGroup in the card selection screen
 */
public class CardGroupScreen : MonoBehaviour, UIElement, IDragTarget
{
    public Button EditButton;
    public Transform ActiveText;
    public Image BackgroundImage;
    public TMPro.TextMeshProUGUI NameText, CardCountText;
    public TMPro.TMP_InputField NameInput;

    protected int Index;
    protected bool bIsEditing, bIsHovered;
    protected CardGroupsScreen Parent;
    protected CardGroup Target;

    public void Init(int Index, CardGroupsScreen Parent, CardGroup Target)
    {
        this.Index = Index;
        this.Parent = Parent;
        this.Target = Target;

        SetName(Target.GetName());
        SetCardCount();
        SetHovered(false);
        SetActive(false);

        Target._OnCardAdded.Add(SetCardCount);
        Target._OnCardRemoved.Add(SetCardCount);
    }

    public void OnDestroy()
    {
        if (Target == null)
            return;

        Target._OnCardAdded.Remove(SetCardCount);
        Target._OnCardRemoved.Remove(SetCardCount);
    }

    public CardGroup GetCardGroupTarget()
    {
        return Target;
    }

    private void SetCardCount(int Count)
    {
        CardCountText.text = Count + CardsText;
    }

    private void SetCardCount()
    {
        if (Target == null)
            return;

        SetCardCount(Target.GetCardCount());
    }

    private void SetName(string Name)
    {
        NameText.text = Name;
        NameInput.text = Name;
        Target.SetName(Name);
    }

    public void OnClickEdit()
    {
        HandleEdit(true);
    }

    public void OnFinishEdit()
    {
        HandleEdit(false);
    }

    private void HandleEdit(bool bIsEditing)
    {
        this.bIsEditing = bIsEditing;

        EditButton.interactable = !bIsEditing;
        NameText.gameObject.SetActive(!bIsEditing);
        NameInput.gameObject.SetActive(bIsEditing);

        if (!bIsEditing)
        {
            SetName(NameInput.text);
        }
        else
        {
            NameInput.text = NameText.text;
        }
    }

    public void OnClickActive()
    {
        Parent.Activate(Index);
    }

    public void StoreCard(Card Card, int Index)
    {
        Target.StoreCard(Card, Index);
    }

    public void SetActive(bool bActive)
    {
        ActiveText.gameObject.SetActive(bActive);
    }

    public void SetSelected(bool Selected) {}

    public void SetHovered(bool Hovered)
    {
        SetHoveredAsParent(Hovered);
        // allow tooltip 
        bIsHovered = Hovered;
    }

    public void SetHoveredAsParent(bool bHovered)
    {
        BackgroundImage.color = bHovered ? HoverColor : NormalColor;

        bool bShowEdit = bHovered && Game.IsIn(Game.GameState.CardSelection);
        EditButton.gameObject.SetActive(bShowEdit);
    }

    public void ClickOn(Vector2 PixelPos){}

    public void Interact() {}

    public bool IsEqual(ISelectable Other)
    {
        if (Other is not CardGroupScreen)
            return false;

        return ((CardGroupScreen)Other).Index == this.Index;
    }

    public bool CanBeLongHovered()
    {
        return !bIsEditing;
    }

    public string GetHoverTooltip() { 
        return Tooltip;
    }

    public RectTransform GetTargetContainer()
    {
        return GetComponent<RectTransform>();
    }

    public RectTransform GetSizeRect()
    {
        return GetComponent<RectTransform>();
    }

    public int GetTargetSiblingIndex(PointerEventData Event)
    {
        return 0;
    }

    public static Color HoverColor = new(1, 1, 1, 0.8f);
    public static Color NormalColor = new(1, 1, 1, 0.6f);

    public static string CardsText = " Cards";
    public static string Tooltip = "Drag and drop cards here to assign them to this card group";
}
