using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MerchantScreen : MonoBehaviour
{
    public MerchantBuyScreen BuyTab;
    public MerchantSellScreen SellTab;
    public GameObject Container;
    public Sprite ActiveTabSprite, InActiveTabSprite;

    private Button BuyButton, SellButton;

    public void Start()
    {
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
        BuyTab.Hide();
        SellTab.Hide();
        Container.SetActive(false);
    }

    public void Show()
    {
        Container.SetActive(true);
        // force initialisation
        BuyTab.Show();
        SellTab.Show();
        OnClickBuy();
    }
}
