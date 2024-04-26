using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/**
 * Represents the ingame menu (escape)
 */
public class IngameMenu : MonoBehaviour
{
    public void ButtonClick()
    {
        if (IsShown)
        {
            Hide();
        }
        else
        {
            Show();
        }
    }

    public void Show()
    {
        _OnOpenBegin?.Invoke();
        IsShown = true;
        ShowElements(IsShown);
        _OnOpenFinish?.Invoke();
    }

    public void Hide()
    {
        IsShown = false;
        ShowElements(IsShown);
        _OnClose?.Invoke();
    }

    private void ShowElements(bool Visible)
    {
        foreach (Transform Child in transform)
        {
            Child.gameObject.SetActive(Visible);
        }
    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ButtonClick();
        }
    }

    public void OnEnable()
    {
        Instance = this;
    }

    public void OnClickExit()
    {
        Game.ExitGame();
    }

    public void OnClickGiveUp()
    {
        Hide();
        Game.Instance.GameOver("You have given up!");
    }

    private bool IsShown = false;

    public delegate void OnOpenBegin();
    public delegate void OnOpenFinish();
    public delegate void OnClose();
    public OnOpenBegin _OnOpenBegin;
    public OnOpenFinish _OnOpenFinish;
    public OnClose _OnClose;

    public static IngameMenu Instance;
}
