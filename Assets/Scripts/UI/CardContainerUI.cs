using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardContainerUI : CardCollection
{
    public Scrollbar VerticalBar;
    public CardUpgradeScreen CardScreen;
    public bool bIsActiveCards = false;

    private float PrevScrollValue = 0;

    public void Start()
    {
        PrevScrollValue = VerticalBar.value;
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
    
    public override void OnLoaded()
    {
        base.OnLoaded();
        CanvasGroup CanvasGroup = GetComponent<CanvasGroup>();
        if (!CanvasGroup)
            return;

        CanvasGroup.alpha = bIsActiveCards ? 1 : 0.8f;
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

    private static float ScrollSpeed = 60f;
}
