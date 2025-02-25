using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameOverScreen : ScreenUI
{
    private bool bIsLeaving = false;
    protected override void Initialize()
    {
        base.Initialize();
        Instance = this;
    }

    public void Leave()
    {
        if (bIsLeaving)
            return;
        if (!Game.TryGetService(out SaveGameManager SaveGameManager))
            return;

        bIsLeaving = true;
        string SaveGame = SaveGameManager.Save(true);
        Game.LoadGame(SaveGame, Game.CardSelectionSceneName, false);
    }

    private void DisplayCurrentRun(Statistics Statistics)
    {
        CurrentRun.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = Statistics.CurrentBuildings + "";
        CurrentRun.transform.GetChild(1).GetComponent<TextMeshProUGUI>().text = Statistics.CurrentMoves + "";
        CurrentRun.transform.GetChild(2).GetComponent<TextMeshProUGUI>().text = Statistics.CurrentUnits + "";
        CurrentRun.transform.GetChild(3).GetComponent<TextMeshProUGUI>().text = Statistics.CurrentResources + "";
        CurrentRun.transform.GetChild(4).GetComponent<TextMeshProUGUI>().text = Statistics.GetHighscore() + "";
    }

    private void DisplayBestRun(Statistics Statistics)
    {
        BestRun.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = Statistics.BestBuildings + "";
        BestRun.transform.GetChild(1).GetComponent<TextMeshProUGUI>().text = Statistics.BestMoves + "";
        BestRun.transform.GetChild(2).GetComponent<TextMeshProUGUI>().text = Statistics.BestUnits + "";
        BestRun.transform.GetChild(3).GetComponent<TextMeshProUGUI>().text = Statistics.BestResources + "";
        BestRun.transform.GetChild(4).GetComponent<TextMeshProUGUI>().text = Statistics.BestHighscore + "";
    }

    public static void GameOver(string Message = null)
    {
        if (!Instance)
            return;

        if (!Game.TryGetService(out Statistics Statistics))
            return;

        if (Message != null)
        {
            Instance.Text.text = Message;
        }

        Instance.DisplayCurrentRun(Statistics);
        Instance.DisplayBestRun(Statistics);
        Instance.Show();
        Instance.bIsLeaving = false;
    }

    private static GameOverScreen Instance;
    public TextMeshProUGUI Text;
    public GameObject CurrentRun, BestRun;
}
