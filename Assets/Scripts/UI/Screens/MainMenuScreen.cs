using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuScreen : MonoBehaviour
{
    void Start()
    {
        Show();
    }

    public void OnSelectNew()
    {
        Game.ModeToStart = Game.GameMode.Game;
        Game.LoadGame(null, Game.MainSceneName, true, TutorialToggle.isOn);
    }

    public void OnSelectMapEditor()
    {
        Game.ModeToStart = Game.GameMode.MapEditor;
        Game.LoadGame(null, Game.MainSceneName, true);
    }

    public void OnSelectExit()
    {
        Game.ExitGame();
    }

    public void OnSelectLoad()
    {
        Hide();
        LoadMenu.Show();
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public LoadMenuScreen LoadMenu;
    public Toggle TutorialToggle;
}
