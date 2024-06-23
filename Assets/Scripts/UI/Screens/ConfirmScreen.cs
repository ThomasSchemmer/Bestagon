using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConfirmScreen : ScreenUI
{
    protected override void Initialize()
    {
        base.Initialize();
        Instance = this;
    }

    public static void Show(string Message, Action Callback)
    {
        Instance.Text.text = Message;
        Instance.ConfirmButton.onClick.RemoveAllListeners();
        Instance.ConfirmButton.onClick.AddListener(() =>
        {
            Callback();
            Instance.Hide();
        });
        Instance.Show();
    }

    public void OnClickCancel()
    {
        Hide();
    }

    public Button ConfirmButton;
    public TextMeshProUGUI Text;
    private static ConfirmScreen Instance;
}
