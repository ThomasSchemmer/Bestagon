using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/** 
 * Provides a general container for cards, that is not directly bound to the player,
 * so not like eg @CardHand. Displays assigned cards in the UI
 */
public class CardContainerUI : CardCollection, IDragTarget
{
    public Scrollbar VerticalBar;
    public CardUpgradeScreen CardScreen;

    private float PrevScrollValue = 0;

    public void Start()
    {
        PrevScrollValue = VerticalBar != null ? VerticalBar.value : 0;
        _OnInit?.Invoke(this);
    }

    public void OnScroll(Vector2 Position)
    {
        // hack to fix https://forum.unity.com/threads/listview-mousewheel-scrolling-speed.1167404/
        // thanks unity..
        float Diff = PrevScrollValue - Position.y; 
        VerticalBar.value -= Diff * ScrollSpeed;
        VerticalBar.value = Mathf.Clamp(VerticalBar.value, 0, 1);
        PrevScrollValue = VerticalBar.value;
        CardScreen.HideCardButtons();
    }
    
    public override void AddCard(Card Card)
    {
        base.AddCard(Card);
        Card.SetCanBeHovered(true);
    }

    public override bool ShouldCardsBeDisplayed()
    {
        return true;
    }

    public override float GetCardSize()
    {
        return 1;
    }

    public override Card.CardState GetState()
    {
        return Card.CardState.DEFAULT;
    }

    public override bool ShouldUpdateCardState()
    {
        return false;
    }

    public RectTransform GetTargetContainer()
    {
        return GetComponent<RectTransform>();
    }

    public RectTransform GetSizeRect()
    {
        // the actual container changes size with the amount of cards it has
        // the viewport will stay the same
        return transform.parent.GetComponent<RectTransform>();
    }

    public int GetTargetSiblingIndex(PointerEventData Event)
    {
        RectTransform Content = GetTargetContainer();
        GridLayoutGroup Layout = Content.GetComponent<GridLayoutGroup>();
        if (!Layout)
            return -1;

        // map pointer to top left inner position of the actual layout
        Vector2 Position = Event.position - new Vector2(Layout.padding.left, Layout.padding.top);
        Position -= new Vector2(Content.position.x, Content.position.y);
        Position.y = -Position.y;
        // then check which index to place at
        Vector2 CellSize = Layout.cellSize + Layout.spacing;
        Vector2Int CellPosition = new();
        CellPosition.x = (int)(Position.x / CellSize.x);
        CellPosition.y = (int)(Position.y / CellSize.y);
        int GlobalPosition = CellPosition.y * Layout.constraintCount + CellPosition.x;
        GlobalPosition = Mathf.Min(GlobalPosition, Content.childCount);
        return GlobalPosition;
    }

    private static float ScrollSpeed = 60f;
}
