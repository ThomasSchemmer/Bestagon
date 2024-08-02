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

    public static void Show(string Message, Action Callback, bool bShowInput = false)
    {
        Instance.Text.text = Message;
        Instance.InputField.gameObject.SetActive(bShowInput);
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

    public static string GetInputText()
    {
        if (!Instance)
            return string.Empty;

        return Instance.InputField.text;
    }

    public TMP_InputField InputField;
    public Button ConfirmButton;
    public TextMeshProUGUI Text;
    private static ConfirmScreen Instance;
}
