using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CardSelectionUI : MonoBehaviour
{
    public TextMeshProUGUI UpgradeText;

    public void Start()
    {
        Game.RunAfterServiceInit((Stockpile Stockpile) =>
        {
            UpdateText(Stockpile.GetUpgradePoints());
        });
    }

    public void OnConfirm()
    {
        if (!Game.TryGetServices(out Stockpile Stockpile, out RelicService RelicService))
            return;

        bool bAllGood = true;
        if (Stockpile.CanAffordUpgrade(1))
        {
            ConfirmScreen.Show("You have unspend upgrade points. Are you sure you want to leave?", ConfirmLeave);
            bAllGood = false;
        }
        if (RelicService.HasIdleSpot())
        {
            ConfirmScreen.Show("You can assign more relics. Are you sure you want to leave?", ConfirmLeave);
            bAllGood = false;
        }

        if (!bAllGood)
            return;

        ConfirmLeave();
    }

    private void ConfirmLeave()
    {
        if (!Game.TryGetService(out SaveGameManager Manager))
            return;

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

        UpdateText(Stockpile.GetUpgradePoints());
    }

}
