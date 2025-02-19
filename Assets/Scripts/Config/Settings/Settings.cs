using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Settings", menuName = "ScriptableObjects/Settings", order = 3)]
public class Settings : ScriptableObject
{
    public SerializedDictionary<SettingName, Setting> Entries = new();


    public Setting this[SettingName Name]
    {
        get
        {
            if (!Entries.ContainsKey(Name))
                return null;

            return Entries[Name];
        }
        set
        {
            if (Entries.ContainsKey(Name))
            {
                Entries[Name] = value;
            }
            else
            {
                Entries.Add(Name, value);
            }
        }
    }

    public void Add(SettingName Name, Setting.Type Type)
    {
        if (Entries.ContainsKey(Name))
            return;

        Entries.Add(Name, new(Type));
    }

    public static Settings Get()
    {
        if (GlobalSettings == null)
        {
            GlobalSettings = Resources.Load("Settings/Settings") as Settings;
        }
        return GlobalSettings;
    }

    public delegate void OnAnySettingChanged(Setting Setting);
    public OnAnySettingChanged _OnAnySettingChanged;

    public static Settings GlobalSettings = null;
}
