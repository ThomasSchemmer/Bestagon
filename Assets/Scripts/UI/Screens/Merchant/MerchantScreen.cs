using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MerchantScreen : ScreenUI
{
    public MerchantBuyScreen BuyTab;
    public MerchantSellScreen SellTab;
    public Sprite ActiveTabSprite, InActiveTabSprite;

    private Button BuyButton, SellButton;

    protected override void Initialize()
    {
        base.Initialize();
        BuyButton = BuyTab?.transform.GetChild(1).GetComponent<Button>();
        SellButton = SellTab?.transform.GetChild(1).GetComponent<Button>();
    }

    public void OnClickBuy()
    {
        BuyButton.image.sprite = ActiveTabSprite;
        SellButton.image.sprite = InActiveTabSprite;
        BuyTab.Show();
        SellTab.Hide();
    }

    public void OnClickSell()
    {
        SellButton.image.sprite = ActiveTabSprite;
        BuyButton.image.sprite = InActiveTabSprite;
        BuyTab.Hide();
        SellTab.Show();
    }

    public void OnClickClose()
    {
        Hide();
    }

    public override void Hide()
    {
        base.Hide();
        BuyTab.Hide();
        SellTab.Hide();
    }

    public override void Show()
    {
        base.Show();
        // force initialisation
        BuyTab.Show();
        SellTab.Show();
        OnClickBuy();
    }
}
