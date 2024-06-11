using System;
using UnityEngine;
using UnityEngine.UI;

/** 
 * Screen showing the player either a choice between two event cards (tribe) or 
 * between a new building and 2 upgrade points (ruins)
 */
public class CollectDecorationScreen : CollectChoiceScreen
{
    private Location CurrentLocation;

    private void Start()
    {
        TokenizedUnitData._OnMovementTo += HandleMovement;
        Container = transform.GetChild(0).gameObject;
        Hide();
    }

    private void OnDestroy()
    {
        TokenizedUnitData._OnMovementTo -= HandleMovement;
    }

    public override void OnSelectOption(int ChoiceIndex)
    {
        base.OnSelectOption(ChoiceIndex);
        Hide();

        if (!Game.TryGetService(out MapGenerator MapGenerator))
            return;

        if (!MapGenerator.TryGetHexagonData(CurrentLocation, out HexagonData HexData))
            return;

        HexData.Decoration = HexagonConfig.HexagonDecoration.None;
        if (!MapGenerator.TryGetHexagon(CurrentLocation, out HexagonVisualization HexVis))
            return;

        HexVis.UpdateMesh();
    }

    private void HandleMovement(Location Location)
    {
        if (!Game.TryGetService(out MapGenerator MapGenerator))
            return;

        if (!MapGenerator.TryGetHexagonData(Location, out HexagonData HexData))
            return;

        if (HexData.Decoration == HexagonConfig.HexagonDecoration.None)
            return;

        CurrentLocation = Location;

        Show();

        switch (HexData.Decoration)
        {
            case HexagonConfig.HexagonDecoration.Ruins: ShowRuinChoices(); break;
            case HexagonConfig.HexagonDecoration.Tribe: ShowTribeChoices(); break;
        }
    }

    private void ShowRuinChoices()
    {
        ChoiceTypes = new() { ChoiceType.Card, ChoiceType.Upgrade};
        Create();
    }

    private void ShowTribeChoices()
    {
        ChoiceTypes = new() { ChoiceType.Card, ChoiceType.Card };
        Create();
    }

    protected override CardDTO.Type GetCardTypeAt(int i)
    {
        switch (GetDecorationType())
        {
            case HexagonConfig.HexagonDecoration.Ruins: return CardDTO.Type.Building;
            case HexagonConfig.HexagonDecoration.Tribe: return CardDTO.Type.Event;
            default: return CardDTO.Type.Event;
        }
    }

    private HexagonConfig.HexagonDecoration GetDecorationType()
    {
        if (!Game.TryGetService(out MapGenerator MapGenerator))
            return HexagonConfig.HexagonDecoration.None;

        if (!MapGenerator.TryGetHexagon(CurrentLocation, out HexagonVisualization Hex))
            return HexagonConfig.HexagonDecoration.None;

        return Hex.Data.Decoration;
    }

    protected override bool ShouldCardBeUnlocked(int i)
    {
        return GetDecorationType() == HexagonConfig.HexagonDecoration.Ruins;
    }


    public void Hide()
    {
        Close();
    }
}
