using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverScreen : MonoBehaviour
{
    void Awake()
    {
        Instance = this;
    }

    public void Leave()
    {
        if (!Game.TryGetService(out SaveGameManager SaveGameManager))
            return;

        UpdateResources();
        UpdateCards();

        string SaveGame = SaveGameManager.Save();
        Game.LoadGame(SaveGame, Game.CardSelectionSceneName, false);
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

    public void Show()
    {
        foreach (Transform Child in transform)
        {
            Child.gameObject.SetActive(true);
        }
    }

    public static void GameOver(string Message = null)
    {
        if (!Instance)
            return;

        if (Message != null)
        {
            Instance.Text.text = Message;
        }
        Instance.Show();
    }

    private static GameOverScreen Instance;
    public TextMeshProUGUI Text;
}
