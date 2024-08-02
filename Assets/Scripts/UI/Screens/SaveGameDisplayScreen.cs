using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SaveGameDisplayScreen : MonoBehaviour
{
    public void CreateSavedGameSlots(SaveGameManager Manager)
    {
        foreach (Transform Child in transform)
        {
            Destroy(Child.gameObject);
        }

        string[] Savegames = Manager.GetSavegameNames();
        foreach (string GameName in Savegames)
        {
            GameObject Slot = Instantiate(SlotPrefab, transform);
            Button Button = Slot.transform.GetChild(0).GetComponent<Button>();
            Button.onClick.AddListener(delegate { Game.LoadGame(GameName, Game.MainSceneName); });
            TMPro.TextMeshProUGUI NameText = Slot.transform.GetChild(1).GetComponent<TMPro.TextMeshProUGUI>();
            NameText.text = SaveGameManager.GetClearName(GameName);
        }
    }

    public GameObject SlotPrefab;
}
