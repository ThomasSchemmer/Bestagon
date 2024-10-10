using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MapSizeEditor : ScreenFeature
{
    public TMPro.TMP_InputField TilesInputField, ChunksInputField;

    private bool bShowAllHexes = false;

    public void Start()
    {
        TilesInputField.text = "" + HexagonConfig.ChunkSize;
        ChunksInputField.text = "" + HexagonConfig.MapMaxChunk;
    }

    public override bool ShouldBeDisplayed()
    {
        return Game.Instance.Mode == Game.GameMode.MapEditor;
    }

    public void OnClick()
    {
        HexagonConfig.ChunkSize = int.Parse(TilesInputField.text);
        HexagonConfig.MapMaxChunk = int.Parse(ChunksInputField.text);
        Game.LoadGame(null, Game.MainSceneName, true);
    }

    public void OnClickToggle()
    {
        bShowAllHexes = !bShowAllHexes; 
        if (!Game.TryGetServices(out MapGenerator MapGen, out SpawnService SpawnSystem))
            return;

        MapGen.ForEachChunk(ToggleChunk);
        SpawnSystem.InitVisibility();
    }

    private void ToggleChunk(ChunkData Chunk)
    {
        Chunk.ForEachHex(ToggleHex);
    }

    private void ToggleHex(HexagonData Data)
    {
        HexagonData.DiscoveryState State = bShowAllHexes ? HexagonData.DiscoveryState.Visited : HexagonData.DiscoveryState.Unknown;
        Data.UpdateDiscoveryState(State, true);
    }
}
