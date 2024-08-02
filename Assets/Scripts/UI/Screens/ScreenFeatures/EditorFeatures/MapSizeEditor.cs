using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapSizeEditor : ScreenFeature
{
    public TMPro.TMP_InputField TilesInputField, ChunksInputField;

    public void Start()
    {
        TilesInputField.text = "" + HexagonConfig.chunkSize;
        ChunksInputField.text = "" + HexagonConfig.mapMaxChunk;
    }

    public override bool ShouldBeDisplayed()
    {
        return Game.Instance.Mode == Game.GameMode.MapEditor;
    }

    public void OnClick()
    {
        HexagonConfig.chunkSize = int.Parse(TilesInputField.text);
        HexagonConfig.mapMaxChunk = int.Parse(ChunksInputField.text);
        Game.LoadGame(null, Game.MainSceneName, true);
    }
}
