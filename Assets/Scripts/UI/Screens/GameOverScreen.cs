using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameOverScreen : ScreenUI
{

    protected override void Initialize()
    {
        base.Initialize();
        Instance = this;
    }

    public void Leave()
    {
        if (!Game.TryGetServices(out SaveGameManager SaveGameManager, out Statistics Statistics))
            return;

        UpdateResources();
        UpdateCards();

        Statistics.ResetCurrentStats();

        string SaveGame = SaveGameManager.Save();
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

    private void UpdateResources()
    {
        if (!Game.TryGetService(out Stockpile Stockpile))
            return;

        Stockpile.ResetResources();
    }

    private void UpdateCards()
    {
        if (!Game.TryGetServices(out CardHand CardHand, out CardStash CardStash, out DiscardDeck DiscardDeck))
            return;
        if (!Game.TryGetService(out CardDeck CardDeck))
            return;

        DiscardDeck.MoveAllCardsTo(CardDeck);
        CardHand.MoveAllCardsTo(CardDeck);

        CardDeck.RefreshAllUsages();
        CardStash.RefreshAllUsages();
        CardStash.DeleteAllCardsConditionally((Card) =>
        {
            return Card.ShouldBeDeleted();
        });
        CardStash.MoveAllCardsConditionallyTo(CardDeck, (Card) =>
        {
            return Card.WasUsedUpThisTurn();
        });
        CardDeck.RefreshAllUsedUps();
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
    }

    private static GameOverScreen Instance;
    public TextMeshProUGUI Text;
    public GameObject CurrentRun, BestRun;
}
