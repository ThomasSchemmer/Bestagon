using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MerchantSellScreen : StockpileScreen
{
    public GameObject Container;
    public GameObject CoinsContainer;

    public void Show()
    {
        Container.SetActive(true);
    }

    public void Hide()
    {
        Container.SetActive(false);
    }

    protected override Transform GetTargetTransform()
    {
        return Container.transform;
    }

    protected override bool ShouldDisplayScouts()
    {
        return false;
    }

    protected override bool ShouldDisplayWorkers()
    {
        return false;
    }

    protected override Vector2 GetTargetOrigin()
    {
        RectTransform RectTrans = Container.GetComponent<RectTransform>();
        return new Vector2(
            StockpileGroupScreen.WIDTH / 2f, 
            RectTrans.sizeDelta.y / 2f - GetContainerSize().y / 3f
        );
    }

    protected override Vector2 GetTargetOffset(int i)
    {
        return new Vector2(
                (GetContainerSize().x + StockpileGroupScreen.OFFSET) * i,
                0
            );
    }

    public override Vector2 GetContainerSize()
    {
        return new Vector2(215, 180);
    }

    public override Vector2 GetElementSize()
    {
        return new Vector2(100, 40);
    }

    public override bool ShouldHeaderBeIcon()
    {
        return false;
    }

    public override void AdaptItemScreen(StockpileGroupScreen GroupScreen, StockpileItemScreen ItemScreen)
    {
        base.AdaptItemScreen(GroupScreen, ItemScreen);

        if (HandleCoinsScreen(ItemScreen))
            return;

        if (!Game.TryGetService(out IconFactory IconFactory))
            return;

        Production.Type Type = (Production.Type)ItemScreen.GetProductionIndex();
        Production Costs = new(Type, COIN_COST);
        GameObject ButtonObject = IconFactory.ConvertVisualsToButton(GroupScreen.GetContainer(), ItemScreen.GetComponent<RectTransform>());
        Button Button = ButtonObject.GetComponent<Button>();
        Button.onClick.RemoveAllListeners();
        Button.onClick.AddListener(() =>
        {
            OnClickSellItem(Button, Type);
        });
        ItemScreen.SetItemSubscription(Type, COIN_COST);

        if (!Game.TryGetService(out Stockpile Stockpile))
            return;

        Button.interactable = Stockpile.CanAfford(Costs);
    }

    private bool HandleCoinsScreen(StockpileItemScreen ItemScreen)
    {
        Production.Type Type = (Production.Type)ItemScreen.GetProductionIndex();
        if (Type != Production.Type.Coins)
            return false;

        ItemScreen.transform.SetParent(CoinsContainer.transform, false);
        ItemScreen.transform.localPosition = Vector3.zero;
        return true;
    }

    private void OnClickSellItem(Button Button, Production.Type Type)
    {
        if (!Game.TryGetService(out Stockpile Stockpile))
            return;

        Production Costs = new(Type, COIN_COST);
        if (!Stockpile.CanAfford(Costs))
            return;

        Stockpile.Pay(Costs);

        Production Reward = new(Production.Type.Coins, COIN_REWARD);
        Stockpile.AddResources(Reward);

        if (Stockpile.CanAfford(Costs))
            return;

        Button.interactable = false;
    }

    private static int COIN_COST = 10;
    private static int COIN_REWARD = 1;
}
