using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/**
 * Wrapper screen for selling and buying cards at the Scavenger
 */
public class ScavengerScreen : ScreenUI
{
    public ScavengerBuyScreen BuyTab;
    public ScavengerSellScreen SellTab;
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
        Game.Instance.OnCloseMenu();

        BuyTab.Hide();
        SellTab.Hide();
    }

    public void Show(Location Location)
    {
        base.Show();
        Game.Instance.OnOpenMenu();
        // force initialisation
        BuyTab.LastLocation = Location;
        BuyTab.Show();
        SellTab.Show();
        OnClickBuy();
    }
}
