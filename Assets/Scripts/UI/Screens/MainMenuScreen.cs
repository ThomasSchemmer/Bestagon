using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainMenuScreen : MonoBehaviour
{
    void Start()
    {
        Show();
    }

    public void OnSelectNew()
    {
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
}
