using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardContainerUI : CardCollection, IDragTarget
{
    public Scrollbar VerticalBar;
    public bool bIsActiveCards = false;

    private float PrevScrollValue = 0;

    public void Start()
    {
        PrevScrollValue = VerticalBar.value;

        if (!Game.TryGetService(out SaveGameManager Manager))
            return;

        Manager.Load();
    }

    public void OnScroll(Vector2 Position)
    {
        // hack to fix https://forum.unity.com/threads/listview-mousewheel-scrolling-speed.1167404/
        // thanks unity..
        float Diff = PrevScrollValue - Position.y; 
        VerticalBar.value -= Diff * ScrollSpeed;
        VerticalBar.value = Mathf.Clamp(VerticalBar.value, 0, 1);
        PrevScrollValue = VerticalBar.value;
    }

    public void OnStartDragOver()
    {

    }

    public void OnStopDragOver()
    {

    }

    private static float ScrollSpeed = 30f;
}
