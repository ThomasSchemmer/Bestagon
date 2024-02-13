using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadMenuScreen : MonoBehaviour
{
    
    public void Hide()
    {
        gameObject.SetActive(false);
        SlotDisplay.gameObject.SetActive(false);
    }

    public void Show()
    {
        if (!Game.TryGetService(out SaveGameManager Manager))
            return;

        gameObject.SetActive(true);
        SlotDisplay.gameObject.SetActive(true);
        SlotDisplay.CreateSavedGameSlots(Manager);
    }

    public void Cancel()
    {
        Hide();
        MainMenu.Show();
    }

    public MainMenuScreen MainMenu;
    public SaveGameDisplayScreen SlotDisplay;
}
