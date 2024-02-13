using System.Collections;
using System.Collections.Generic;
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
        if (!Game.TryGetService(out CardHand CardHand))
            return;
        if (!Game.TryGetService(out CardDeck CardDeck))
            return;

        CardHand.MoveAllCardsTo(CardDeck);

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

    public static GameOverScreen Instance;
}
