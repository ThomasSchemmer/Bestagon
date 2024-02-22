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
        if (!Game.TryGetServices(out CardHand CardHand, out CardStash CardStash, out DiscardDeck DiscardDeck))
            return;
        if (!Game.TryGetService(out CardDeck CardDeck))
            return;

        DiscardDeck.MoveAllCardsTo(CardDeck);
        CardHand.MoveAllCardsTo(CardDeck);

        CardDeck.RefreshAllUsages();
        CardStash.RefreshAllUsages();

        SaveGameManager.Save();
        Game.LoadGame(null, "CardSelection", false);
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
