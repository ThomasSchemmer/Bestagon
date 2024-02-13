using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardUpgradeScreen : GameService
{
    public RectTransform UpgradeButton;
    public GameObject TargetCardScreen, UpgradeArrow, ConfirmButton;
    public Canvas Canvas;
    public List<GameObject> ToHide = new();
    public List<GameObject> ToDim = new();

    private Card LastCard = null, CopyCard = null;
    private BuildingData UpgradedBuildingData;

    public enum UpgradeableAttributes
    {
        AffectedTiles,
        Production,
        Range,
        ProductionIncrease,
        BuildableTiles,
        MaxUsages,
        MaxWorker
    }

    public void Cancel()
    {
        Hide();
    }

    public void Show()
    {
        gameObject.SetActive(true);
        foreach (GameObject GO in ToHide)
        {
            GO.SetActive(false);
        }
        foreach (GameObject GO in ToDim)
        {
            GO.GetComponent<CanvasGroup>().alpha = 0.8f;
        }
        TargetCardScreen.SetActive(false);
        UpgradeArrow.SetActive(false);
        LoadCard();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        foreach (GameObject GO in ToHide)
        {
            GO.SetActive(true);
        }
        foreach (GameObject GO in ToDim)
        {
            GO.GetComponent<CanvasGroup>().alpha = 1;
        }
        TargetCardScreen.SetActive(false);
        UpgradeArrow.SetActive(false);
        ConfirmButton.SetActive(false);
    }

    public void ShowButtonAtCard(Card Card, bool bIsVisible) {
        RectTransform RectTransform = Card.GetComponent<RectTransform>();
        Vector3 TargetPosition = RectTransform.position;
        TargetPosition.x += 100 / 2f;
        TargetPosition.y += 175 / 2f;
        Vector3 OffsetWorld = TargetPosition - UpgradeButton.position;
        Vector3 OffsetLocal = UpgradeButton.InverseTransformVector(OffsetWorld);
        UpgradeButton.anchoredPosition = UpgradeButton.anchoredPosition + (Vector2)OffsetLocal;
        UpgradeButton.gameObject.SetActive(bIsVisible);

        LastCard = Card;
    }

    public void HideUpgradeButton()
    {
        UpgradeButton.gameObject.SetActive(false);
    }

    public void LoadCard()
    {
        LoadTextForCard(LastCard, false);
    }

    private void LoadTextForCard(Card Card, bool bIsForUpgraded)
    {
        Transform CardTransform = transform.GetChild(bIsForUpgraded ? 4 : 3);
        TextMeshProUGUI NameText = CardTransform.GetChild(1).GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI SymbolText = CardTransform.GetChild(2).GetComponent<TextMeshProUGUI>();

        Transform UsagesTransform = bIsForUpgraded ? CardTransform.GetChild(3) : CardTransform.GetChild(3).GetChild(0);
        Transform MaxWorkerTransform = bIsForUpgraded ? CardTransform.GetChild(4) : CardTransform.GetChild(4).GetChild(0);

        TextMeshProUGUI UsagesText = UsagesTransform.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI MaxWorkerText = MaxWorkerTransform.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI CostsText = CardTransform.GetChild(5).GetChild(0).GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI EffectText = CardTransform.GetChild(6).GetChild(0).GetComponent<TextMeshProUGUI>();

        if (!bIsForUpgraded)
        {
            Button UsagesButton = CardTransform.GetChild(3)?.GetComponent<Button>();
            Button MaxWorkerButton = CardTransform.GetChild(4)?.GetComponent<Button>();
            UsagesButton.onClick.RemoveAllListeners();
            MaxWorkerButton.onClick.RemoveAllListeners();
            UsagesButton.onClick.AddListener(delegate { SelectUpgrade(UpgradeableAttributes.MaxUsages); });
            MaxWorkerButton.onClick.AddListener(delegate { SelectUpgrade(UpgradeableAttributes.MaxWorker); });
        }

        NameText.SetText(Card.GetName());
        SymbolText.SetText(Card.GetSymbol());
        UsagesText.SetText(Card.GetUsages());
        MaxWorkerText.SetText(Card.GetMaxWorkers());
        CostsText.SetText(Card.GetCostText());
        EffectText.SetText(Card.GetDescription());
    }

    private void SelectUpgrade(UpgradeableAttributes SelectedUpgrade)
    {
        UpgradedBuildingData = Instantiate(LastCard.GetBuildingData());
        UpgradedBuildingData.Upgrade(SelectedUpgrade);
        TargetCardScreen.SetActive(true);
        UpgradeArrow.SetActive(true);
        ConfirmButton.SetActive(true);

        if (CopyCard != null)
        {
            Destroy(CopyCard.gameObject);
        }
        CopyCard = Instantiate(LastCard);
        CopyCard.SetBuildingData(UpgradedBuildingData);
        LoadTextForCard(CopyCard, true);
    }

    public void ConfirmUpgrade()
    {
        Destroy(CopyCard.gameObject);
        LastCard.SetBuildingData(UpgradedBuildingData);
        LastCard.GenerateCard();
        Hide();
    }

    protected override void StartServiceInternal() {}

    protected override void StopServiceInternal() {}
}
