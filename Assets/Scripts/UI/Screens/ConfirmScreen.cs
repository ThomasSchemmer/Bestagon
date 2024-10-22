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
        if (Instance == null)
            return;

        Instance.Show(Message, Callback, null, string.Empty, bShowInput);
    }

    public static void Show(string Message, Action Callback, Action OtherAction, string OtherName)
    {
        Instance.Show(Message, Callback, OtherAction, OtherName, false);
    }

    private void Show(string Message, Action Callback, Action OtherAction, string OtherName, bool bShowInput)
    {
        if (Text == null)
            return;

        bool bHasOtherAction = OtherAction != null;
        ModifyAs(bShowInput, bHasOtherAction);

        Text.text = Message;

        ConfirmButton.onClick.RemoveAllListeners();
        ConfirmButton.onClick.AddListener(() =>
        {
            Callback();
            Instance.Hide();
        });

        if (bHasOtherAction)
        {
            OtherButton.onClick.RemoveAllListeners();
            OtherButton.onClick.AddListener(() =>
            {
                OtherAction();
                Instance.Hide();
            });
            OtherButton.transform.GetChild(0).GetComponent<TMPro.TextMeshProUGUI>().text = OtherName;
        }

        Show();
    }

    public void OnClickCancel()
    {
        Hide();
    }

    private void ModifyAs(bool bShowInput, bool bHasOtherButton)
    {
        RectTransform TextTransform = Text.GetComponent<RectTransform>();
        TextTransform.sizeDelta = new (TextTransform.sizeDelta.x, bShowInput ? 40 : 80);
        TextTransform.anchoredPosition = new(TextTransform.anchoredPosition.x, bShowInput ? 35 : 25);
        
        InputField.gameObject.SetActive(bShowInput);
        OtherButton.gameObject.SetActive(bHasOtherButton);
    }

    public static string GetInputText()
    {
        if (!Instance)
            return string.Empty;

        return Instance.InputField.text;
    }

    public TMP_InputField InputField;
    public Button ConfirmButton;
    public Button OtherButton;
    public TextMeshProUGUI Text;
    private static ConfirmScreen Instance;
}
