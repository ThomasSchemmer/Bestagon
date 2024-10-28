using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SaveGameDisplayScreen : MonoBehaviour
{
    public void CreateSavedGameSlots()
    {
        foreach (Transform Child in transform)
        {
            Destroy(Child.gameObject);
        }

        string[] Savegames = SaveGameManager.GetSavegameNames();
        foreach (string GameName in Savegames)
        {
            GameObject Slot = Instantiate(SlotPrefab, transform);
            Button Button = Slot.transform.GetChild(0).GetComponent<Button>();
            Button.onClick.RemoveAllListeners();
            Button.onClick.AddListener(() => {
                LoadGame(GameName);
                });
            TMPro.TextMeshProUGUI NameText = Slot.transform.GetChild(1).GetComponent<TMPro.TextMeshProUGUI>();
            NameText.text = SaveGameManager.GetClearName(GameName);
        }
    }

    private void LoadGame(string GameName)
    {
        Game.ModeToStart = Game.GameMode.Game;
        Game.LoadGame(GameName, Game.MainSceneName);
    }

    public GameObject SlotPrefab;
}
