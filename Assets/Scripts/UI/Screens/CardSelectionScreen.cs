using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CardSelectionScreen : MonoBehaviour
{
    public TextMeshProUGUI UpgradeText;

    public void Start()
    {
        Game.RunAfterServiceInit((Stockpile Stockpile) =>
        {
            UpdateText(Stockpile.UpgradePoints);
        });
    }

    public void OnConfirm()
    {
        if (!Game.TryGetServices(out SaveGameManager Manager, out Stockpile Stockpile))
            return;

        Stockpile.ResetResources();

        // write into temp and then trigger a reload through the scene load
        string FileToLoad = Manager.Save();
        Game.LoadGame(FileToLoad, Game.MainSceneName);
    }

    public void UpdateText(int UpgradePoints)
    {
        UpgradeText.text = "Upgrades available " + UpgradePoints;
    }

    public void UpdateText()
    {
        if (!Game.TryGetService(out Stockpile Stockpile))
            return;

        UpdateText(Stockpile.UpgradePoints);
    }

}
