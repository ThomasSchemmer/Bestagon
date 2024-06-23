using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/**
 * Represents the ingame menu (escape)
 */
public class IngameMenuScreen : ScreenUI
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

    public override void Show()
    {
        base.Show();
        _OnOpenBegin?.Invoke();
        IsShown = true;
        _OnOpenFinish?.Invoke();
    }

    public override void Hide()
    {
        base.Hide();
        IsShown = false;
        _OnClose?.Invoke();
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

        Action A = () =>
        {
            Game.ExitGame();
        };
        ConfirmScreen.Show("Are you sure you want exit the game? Any unsaved progress will be lost!", A);
    }

    private bool IsShown = false;

    public delegate void OnOpenBegin();
    public delegate void OnOpenFinish();
    public delegate void OnClose();
    public OnOpenBegin _OnOpenBegin;
    public OnOpenFinish _OnOpenFinish;
    public OnClose _OnClose;

    public static IngameMenuScreen Instance;
}
