using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
/** Handles user preferences */
public class Settings : SaveableService
{
    public List<Setting> List = new();

    [SaveableDictionary]
    private SerializedDictionary<int, Setting> SettingsInternal = new();

    protected override void StartServiceInternal() {
        Game.RunAfterServiceInit((SaveGameManager Manager) =>
        {
            if (Manager.HasDataFor(SaveGameType.Settings))
                return;

            FillInternalSettings();
            _OnInit?.Invoke(this);
        });
    }

    public override void OnAfterLoaded()
    {
        base.OnAfterLoaded();
        _OnInit?.Invoke(this);
    }

    protected override void StopServiceInternal()
    {}

    private void FillInternalSettings()
    {
        SettingsInternal.Clear();
        foreach (var Setting in List)
        {
            SettingsInternal.Add(Setting.Name.GetHashCode(), Setting);
        }
    }

    public bool TryGetValue(string Key, out object Value)
    {
        int Hash = Key.GetHashCode();
        Value = default;
        if (!SettingsInternal.ContainsKey(Hash))
            return false;

        Value = SettingsInternal[Hash].GetValue();
        return true;
    }

    public void AddSetting(Setting.SettingType Type)
    {
        switch (Type)
        {
            case Setting.SettingType.boolean: List.Add(new BooleanSetting()); break;
        }
    }

}
