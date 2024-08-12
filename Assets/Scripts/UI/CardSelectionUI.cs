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
            UpdateText(Stockpile.UpgradePoints);
        });
    }

    public void OnConfirm()
    {
        if (!Game.TryGetService(out Stockpile Stockpile))
            return;

        if (Stockpile.UpgradePoints > 0)
        {

            ConfirmScreen.Show("You have unspend upgrade points. Are you sure you want to leave?", ConfirmLeave);
        }
        else
        {
            ConfirmLeave();
        }
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

        UpdateText(Stockpile.UpgradePoints);
    }

}
