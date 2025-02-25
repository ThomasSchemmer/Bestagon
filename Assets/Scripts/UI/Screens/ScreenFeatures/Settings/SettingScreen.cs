using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SettingScreen : MonoBehaviour
{
    private int Index = -1;
    private Setting Setting;
    private SettingName Name;
    private TMPro.TextMeshProUGUI NameText, ValueText;

    public void Init(Setting Setting, SettingName Name, int Index)
    {
        this.Index = Index;
        this.Setting = Setting;
        this.Name = Name;
        LinkVisuals();
    }

    private void LinkVisuals()
    {
        NameText = transform.GetChild(0).GetComponent<TMPro.TextMeshProUGUI>();
        NameText.text = Name.ToString();

        switch (Setting._Type)
        {
            case Setting.Type.Boolean: LinkBoolVisuals(); break;
            case Setting.Type.Int: LinkIntVisuals(); break;
        }
    }

    private void LinkIntVisuals() 
    { 
        ValueText = transform.GetChild(2).GetComponent<TMPro.TextMeshProUGUI>();
        var Slider = transform.GetChild(1).GetComponent<Slider>();
        Slider.onValueChanged.AddListener((Value) => OnValueChanged(Value));
        Slider.value = Setting.Value;
        Slider.minValue = Setting.MinValue;
        Slider.maxValue = Setting.MaxValue;
        ValueText.text = "" + Setting.Value;
    }

    private void OnValueChanged(float Value)
    {
        Value = Mathf.Clamp(Value, Setting.MinValue, Setting.MaxValue);
        Setting.Value = (int)Value;
        ValueText.text = "" + Setting.Value;
    }

    private void LinkBoolVisuals()
    {
        var Toggle = transform.GetChild(1).GetComponent<Toggle>();
        Toggle.onValueChanged.AddListener((isOn) => OnValueChanged(isOn));
        Toggle.isOn = Setting.Value > 0;
    }

    private void OnValueChanged(bool Value)
    {
        Setting.Value = Value ? 1 : 0;
    }
}
