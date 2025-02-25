using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/** 
 * Handles the creation and display of @CardGroupScreens and is the access point between 
 * them and the @CardGroupManager
 */
public class CardGroupsScreen : ScreenUI
{
    public GameObject ShowScreenButton;
    public Transform CardGroupContainer;

    protected List<CardGroupScreen> Screens;
    protected int OldActive = -1;

    public void CreateCardGroupScreens(CardGroupManager Manager, int ActiveGroup)
    {
        if (Container == null)
        {
            Initialize();
        }

        if (!Game.TryGetService(out IconFactory IconFactory))
            return;

        Game.TryGetService(out DraggableManager DraggableManager);
        
        Screens = new List<CardGroupScreen>();
        for (int i = 0; i < Manager.GetCardGroupCount(); i++)
        {
            CardGroupScreen Screen = IconFactory.CreateCardGroupScreen(CardGroupContainer);
            Screen.Init(i, this, Manager.GetCardGroup(i));
            RectTransform Rect = Screen.GetComponent<RectTransform>();
            Rect.anchoredPosition = GetPosition(i);
            Screens.Add(Screen);

            //dragging is only used in CardSelection
            if (DraggableManager == null)
                continue;
            DraggableManager.Targets.Add(Screen.GetComponent<RectTransform>());
        }

        Activate(ActiveGroup);
    }

    private Vector2 GetPosition(int i)
    {
        if (Game.IsIn(Game.GameState.CardSelection))
        {
            return CardSelectionPosition + CardSelectionOffset * i;
        }
        else{
            Vector2 Offset = new(
                    MainOffset.x * ((i % 2) == 0 ? -1 : 1),
                    MainOffset.y * (i < 2 ? 1 : -1)
                );
            return MainPosition + Offset;
        }
    }

    public void Activate(int Index)
    {
        if (OldActive >= 0)
        {
            Screens[OldActive].SetActive(false);
        }
        Screens[Index].SetActive(true);
        OldActive = Index;

        if (!Game.TryGetService(out CardGroupManager CardManager))
            return;

        CardManager.SwitchTo(Index, true);
    }

    public CardGroupScreen GetCardGroupScreen(int i) {
        return Screens[i];
    }

    public int GetCardGroupScreenCount()
    {
        return Screens.Count;
    }

    public void OnToggle()
    {
        if (Container == null)
            return;

        if (Container.activeSelf)
        {
            Hide();
        }
        else
        {
            Show();
        }
    }

    public void DisplayScreenButton(bool bShow)
    {
        if (ShowScreenButton == null)
            return;

        ShowScreenButton.SetActive(bShow);
    }

    public static Vector2 CardSelectionPosition = new(50, 250);
    public static Vector2 CardSelectionOffset = new(0, -135);

    public static Vector2 MainPosition = new(0, 0);
    public static Vector2 MainOffset = new(185, 80);
}
