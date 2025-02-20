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

        Instance.Show(Message, Callback, null, string.Empty, bShowInput, false);
    }

    public static void Show(string Message, Action Callback, Action OtherAction, string OtherName, bool bIsCancelAction)
    {
        Instance.Show(Message, Callback, OtherAction, OtherName, false, bIsCancelAction);
    }

    private void Show(string Message, Action Callback, Action OtherAction, string OtherName, bool bShowInput, bool bIsCancelAction)
    {
        if (Text == null)
            return;

        bool bHasOtherAction = OtherAction != null;
        ModifyAs(bShowInput, bHasOtherAction, bIsCancelAction);

        Text.text = Message;

        ConfirmButton.onClick.RemoveAllListeners();
        ConfirmButton.onClick.AddListener(() =>
        {
            Callback();
            Instance.Hide();
        });

        if (bHasOtherAction)
        {
            Button Target = bIsCancelAction ? CancelButton : OtherButton;
            Target.onClick.RemoveAllListeners();
            Target.onClick.AddListener(() =>
            {
                OtherAction();
                Instance.Hide();
            });
            Target.transform.GetChild(0).GetComponent<TMPro.TextMeshProUGUI>().text = bIsCancelAction ? "Cancel" : OtherName;
        }

        Show();
    }

    public void OnClickCancel()
    {
        Hide();
    }

    private void ModifyAs(bool bShowInput, bool bHasOtherButton, bool bIsCancelAction)
    {
        RectTransform TextTransform = Text.GetComponent<RectTransform>();
        TextTransform.sizeDelta = new (TextTransform.sizeDelta.x, bShowInput ? 40 : 80);
        TextTransform.anchoredPosition = new(TextTransform.anchoredPosition.x, bShowInput ? 35 : 25);
        
        InputField.gameObject.SetActive(bShowInput);
        OtherButton.gameObject.SetActive(bHasOtherButton && !bIsCancelAction);
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
    public Button CancelButton;
    public TextMeshProUGUI Text;
    private static ConfirmScreen Instance;
}
