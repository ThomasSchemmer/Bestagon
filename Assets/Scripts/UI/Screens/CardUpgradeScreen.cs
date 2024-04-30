using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardUpgradeScreen : GameService
{
    public RectTransform UpgradeButton;
    public GameObject RegularCardContainer, UpgradedCardContainer, UpgradeArrow, ConfirmButton;
    public CardSelectionScreen CardSelectionScreen;
    public GameObject UpgradeButtonPrefab;
    public List<GameObject> ToHide = new();
    public List<GameObject> ToDim = new();

    //for now only BuildingCards can be upgraded
    private BuildingCard LastCard = null, CopyCard = null, UpgradedCard = null;
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
        UpgradedCardContainer.SetActive(false);
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
        UpgradedCardContainer.SetActive(false);
        UpgradeArrow.SetActive(false);
        EnableConfirmButton(false);
        if (CopyCard)
        {
            Destroy(CopyCard.gameObject);
        }
        if (UpgradedCard)
        {
            Destroy(UpgradedCard.gameObject);
        }
    }

    public void ShowButtonAtCard(Card Card, bool bIsVisible) {

        bIsVisible = Card is BuildingCard ? bIsVisible : false;
        bool bHaveEnoughUpgrades = Game.TryGetService(out Stockpile Stockpile) && Stockpile.UpgradePoints > 0;

        RectTransform RectTransform = Card.GetComponent<RectTransform>();
        Vector3 TargetPosition = RectTransform.position;
        TargetPosition.x += 200 / 2f - 15;
        TargetPosition.y += 320 / 2f - 15;
        Vector3 OffsetWorld = TargetPosition - UpgradeButton.position;
        Vector3 OffsetLocal = UpgradeButton.InverseTransformVector(OffsetWorld);
        UpgradeButton.anchoredPosition = UpgradeButton.anchoredPosition + (Vector2)OffsetLocal;
        UpgradeButton.gameObject.SetActive(bIsVisible && bHaveEnoughUpgrades);

        LastCard = Card as BuildingCard;
    }

    public void HideUpgradeButton()
    {
        UpgradeButton.gameObject.SetActive(false);
    }

    public void LoadCard()
    {
        CopyCard = Instantiate(LastCard);
        GameObject Clone = CopyCard.gameObject;
        MoveToContainer(Clone, false);
        ConvertToButton(Clone, LastCard.GetBuildingData(), CopyCard.GetMaxWorkerTransform(), UpgradeableAttributes.MaxWorker);
        ConvertToButton(Clone, LastCard.GetBuildingData(), CopyCard.GetUsagesTransform(), UpgradeableAttributes.MaxUsages);
        ConvertToButton(Clone, LastCard.GetBuildingData(), CopyCard.GetProductionTransform(), UpgradeableAttributes.Production);
        EnableConfirmButton(false);
    }

    private void MoveToContainer(GameObject NewCard, bool bIsForUpgraded)
    {
        Transform TargetTransform = bIsForUpgraded ? UpgradedCardContainer.transform : RegularCardContainer.transform;
        NewCard.transform.SetParent(TargetTransform, false);
        RectTransform CloneTransform = NewCard.GetComponent<RectTransform>();
        CloneTransform.anchoredPosition = Vector2.zero;
        CloneTransform.anchorMin = Vector2.one * 0.5f;
        CloneTransform.anchorMax = Vector2.one * 0.5f;
    }

    private void ConvertToButton(GameObject Clone, BuildingData BuildingData, Transform UsagesTransform, UpgradeableAttributes Type)
    {
        if (!BuildingData.IsUpgradePossible(Type))
            return;

        RectTransform OldUsagesVisuals = UsagesTransform.GetChild(0).GetComponent<RectTransform>();

        Button UsagesButton = Instantiate(UpgradeButtonPrefab).GetComponent<Button>();
        RectTransform NewUsagesVisuals = UsagesButton.GetComponent<RectTransform>();
        NewUsagesVisuals.anchorMin = OldUsagesVisuals.anchorMin;
        NewUsagesVisuals.anchorMax = OldUsagesVisuals.anchorMax;
        NewUsagesVisuals.sizeDelta = OldUsagesVisuals.sizeDelta;
        NewUsagesVisuals.SetParent(UsagesTransform, false);
        NewUsagesVisuals.anchoredPosition = OldUsagesVisuals.anchoredPosition;
        OldUsagesVisuals.SetParent(NewUsagesVisuals, true);

        UsagesButton.onClick.RemoveAllListeners();
        UsagesButton.onClick.AddListener(delegate { SelectUpgrade(Type); });
    }

    private void EnableConfirmButton(bool bIsEnabled) { 
        ConfirmButton.GetComponent<Button>().interactable = bIsEnabled;
    }

    private void SelectUpgrade(UpgradeableAttributes SelectedUpgrade)
    {
        UpgradedBuildingData = Instantiate(LastCard.GetBuildingData());
        UpgradedBuildingData.Upgrade(SelectedUpgrade);
        UpgradedCardContainer.SetActive(true);
        UpgradeArrow.SetActive(true);
        ConfirmButton.SetActive(true);
        EnableConfirmButton(true);

        if (UpgradedCard != null)
        {
            Destroy(UpgradedCard.gameObject);
        }
        UpgradedCard = Instantiate(LastCard);
        UpgradedCard.SetBuildingData(UpgradedBuildingData);
        UpgradedCard.GenerateCard();
        MoveToContainer(UpgradedCard.gameObject, true);
    }

    public void ConfirmUpgrade()
    {
        if (!Game.TryGetService(out Stockpile Stockpile))
            return;

        Destroy(CopyCard.gameObject);
        LastCard.SetBuildingData(UpgradedBuildingData);
        LastCard.GenerateCard();
        Hide();
        Stockpile.UpgradePoints -= 1;
        CardSelectionScreen.UpdateText();
    }

    protected override void StartServiceInternal() {}

    protected override void StopServiceInternal() {}
}
