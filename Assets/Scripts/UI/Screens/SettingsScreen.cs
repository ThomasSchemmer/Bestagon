using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/** 
 * Screeen to dynamically display the currently available @Settings to the user 
 */
public class SettingsScreen : ScreenUI
{
    public RectTransform SettingContainer;
    private Settings Settings;
    List<SettingScreen> SettingScreens = new();

    private void CreateScreens()
    {
        Settings = Settings.Get();
        int Count = 0;
        foreach (var Tuple in Settings.Entries)
        {
            CreateFeature(Tuple.Value, Tuple.Key, Count);
        }
    }

    private void CreateFeature(Setting Setting, SettingName Name, int i)
    {
        if (!Game.TryGetService(out IconFactory IconFactory))
            return;

        GameObject GO = IconFactory.GetVisualsForSetting(Setting);
        GO.transform.SetParent(SettingContainer, false);
        SettingScreen Feature = GO.AddComponent<SettingScreen>();
        Feature.Init(Setting, Name, i);
        SettingScreens.Add(Feature);
    }

    public void Start()
    {
        Game.RunAfterServiceInit((IconFactory IconFactory) =>
        {
            Initialize();
            CreateScreens();
            Hide();
        });
    }
}
