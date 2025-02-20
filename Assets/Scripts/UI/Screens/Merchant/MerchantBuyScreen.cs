using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MerchantBuyScreen : CollectChoiceScreen
{
    public GameObject CostContainer;

    private void Start()
    {
        Initialize();
        bCloseOnPick = false;
        bDestroyOnPick = false;

        if (!Game.TryGetService(out IconFactory IconFactory))
            return;

        Production RefreshCosts = new Production(Production.Type.Coins, REFRESH_COST);
        GameObject RefreshCostsGO = IconFactory.GetVisualsForProduction(RefreshCosts, null, true).gameObject;
        RefreshCostsGO.transform.SetParent(CostContainer.transform, false);
    }

    public void OnClickRefresh()
    {
        if (!Game.TryGetService(out Stockpile Stockpile))
            return;

        Production RefreshCosts = new Production(Production.Type.Coins, REFRESH_COST);
        if (!Stockpile.CanAfford(RefreshCosts))
            return;

        Stockpile.Pay(RefreshCosts);
        Refresh();
    }

    private void Refresh()
    {
        int Count = Choices != null ? Choices.Length : 0;
        for(int i = 0; i < Count; i++)
        {
            DestroyChoice(Choices[i]);
        }

        Create();
    }

    public override void Show()
    {
        base.Show();
        if (Choices == null || Choices.Length == 0)
        {
            Create();
        }
    }

    protected override CardDTO.Type GetCardTypeAt(int i)
    {
        return CardDTO.Type.Building;
    }

    protected override bool ShouldCardBeUnlocked(int i)
    {
        return false;
    }

    protected override void SetChoiceBuilding(int i, Card Card)
    {
        base.SetChoiceBuilding(i, Card);

        if (Card is not BuildingCard BuildingCard)
            return;

        BuildingCard.GetBuildingData().Corrupt();
        BuildingCard.Show(Card.Visibility.Visible);

        if (!Game.TryGetService(out IconFactory IconFactory))
            return;

        Transform TargetContainer = ChoiceContainers[i];
        Transform CostContainer = TargetContainer.GetChild(0).GetChild(5);
        Production Costs = GetCostsForChoice(i);
        GameObject CostsGO = IconFactory.GetVisualsForProduction(Costs, Card, true).gameObject;
        CostsGO.transform.SetParent(CostContainer, false);
    }

    protected override Production GetCostsForChoice(int i)
    {
        int Costs = (int)AttributeSet.Get()[AttributeType.MarketCosts].CurrentValue;
        return new Production(Production.Type.Coins, Costs);
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
        UnityEngine.Random.InitState(Time.frameCount);
        return UnityEngine.Random.Range(0, 100);
    }

    protected override int GetWorkerCostsForChoice(int i)
    {
        return 0;
    }

    protected override int GetXOffsetBetweenChoices()
    {
        return 0;
    }

    private static int REFRESH_COST = 10;
}
