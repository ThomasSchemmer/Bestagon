using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CardSelectionScreen : MonoBehaviour
{
    public void Start()
    {
        if (!Game.TryGetService(out SaveGameManager Manager))
            return;

        Manager.TryLoad();
    }

    public void OnConfirm()
    {
        if (!Game.TryGetService(out SaveGameManager Manager))
            return;

        // write into temp and then trigger a reload through the scene load
        string FileToLoad = Manager.Save();
        Game.LoadGame(FileToLoad, "Main");
    }
}
