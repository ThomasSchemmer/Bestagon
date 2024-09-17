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
        TokenizedUnitEntity._OnMovementTo += HandleMovement;
        Container = transform.GetChild(0).gameObject;
        Hide();
    }

    private void OnDestroy()
    {
        TokenizedUnitEntity._OnMovementTo -= HandleMovement;
    }

    public override void OnSelectOption(int ChoiceIndex)
    {
        base.OnSelectOption(ChoiceIndex);
        Hide();

        if (!Game.TryGetServices(out MapGenerator MapGenerator, out DecorationService DecorationService))
            return;

        if (!MapGenerator.TryGetHexagonData(CurrentLocation, out HexagonData HexData))
            return;

        if (!DecorationService.TryGetEntityAt(CurrentLocation, out var Decoration))
            return;

        DecorationService.KillEntity(Decoration);

        if (!MapGenerator.TryGetChunkVis(CurrentLocation, out var ChunkVis))
            return;

        ChunkVis.RefreshTokens();
    }

    private void HandleMovement(Location Location)
    {
        if (!Game.TryGetService(out DecorationService DecSer))
            return;

        if (!DecSer.TryGetEntityAt(Location, out DecorationEntity Decoration))
            return;

        CurrentLocation = Location;

        Show();

        switch (Decoration.DecorationType)
        {
            case DecorationEntity.DType.Ruins: ShowRuinChoices(); break;
            case DecorationEntity.DType.Tribe: ShowTribeChoices(); break;
            case DecorationEntity.DType.Treasure: ShowTreasureChoices(); break;
        }
    }

    private void ShowRuinChoices()
    {
        ChoiceTypes = new() { ChoiceType.Card, ChoiceType.Upgrade};
        Create();
    }

    private void ShowTreasureChoices()
    {
        ChoiceTypes = new() { ChoiceType.Relic, ChoiceType.Relic };
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
            case DecorationEntity.DType.Ruins: return CardDTO.Type.Building;
            case DecorationEntity.DType.Tribe: return CardDTO.Type.Event;
            default: return CardDTO.Type.Event;
        }
    }

    private DecorationEntity.DType GetDecorationType()
    {
        if (!Game.TryGetService(out DecorationService DecorationService))
            return default;

        if (!DecorationService.TryGetEntityAt(CurrentLocation, out DecorationEntity Entity))
            return default;

        return Entity.DecorationType;
    }

    protected override bool ShouldCardBeUnlocked(int i)
    {
        return GetDecorationType() == DecorationEntity.DType.Ruins;
    }

    protected override Production GetCostsForChoice(int i)
    {
        return Production.Empty;
    }

    protected override int GetUpgradeCostsForChoice(int i)
    {
        return 0;
    }

    protected override CardCollection GetTargetCardCollection()
    {
        return Game.GetService<CardHand>();
    }

    protected override int GetSeed()
    {
        return CurrentLocation.GetHashCode();
    }
}
