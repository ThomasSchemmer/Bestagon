using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.UI;

public class CardUpgradeScreen : GameService
{
    public RectTransform UpgradeButton;
    public RectTransform PinButton;
    public GameObject RegularCardContainer, UpgradedCardContainer, UpgradeArrow, ConfirmButton;
    public CardSelectionUI CardSelectionUI;
    public GameObject UpgradeButtonPrefab;
    public List<GameObject> ToHide = new();
    public List<GameObject> ToDim = new();
    public Sprite PinInactive, PinActive;

    //for now only BuildingCards can be upgraded
    private Card LastCard = null;
    private BuildingCard CopyCard = null, UpgradedCard = null;
    private BuildingEntity UpgradedBuildingData;

    public enum UpgradeableAttributes
    {
        Production,
        Range,
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
        HideCardButtons();
    }

    public void ShowButtonsAtCard(Card Card, bool bIsVisible) {

        bIsVisible = Game.TryGetService(out DraggableManager DraggableManager) && !DraggableManager.IsDragging() ? bIsVisible : false;
        ShowUpgradeButtonAtCard(Card, bIsVisible);
        ShowPinButtonAtCard(Card, bIsVisible);

        LastCard = Card;
    }

    private void ShowUpgradeButtonAtCard(Card Card, bool bIsVisible)
    {
        if (Card == null)
        {
            HideCardButtons();
            return;
        }

        bool bIsBuildingCard = Card is BuildingCard;
        bool bHaveEnoughUpgrades = Game.TryGetService(out Stockpile Stockpile) && Stockpile.CanAffordUpgrade(1);
        bool bCanBeUpgraded = Card.CanBeUpgraded();
        bIsVisible = bIsVisible && bIsBuildingCard && bHaveEnoughUpgrades && bCanBeUpgraded;

        RectTransform RectTransform = Card.GetComponent<RectTransform>();
        Vector3 TargetPosition = RectTransform.position;
        TargetPosition.x += 200 / 2f - 15 - 35;
        TargetPosition.y += 320 / 2f - 15;
        Vector3 OffsetWorld = TargetPosition - UpgradeButton.position;
        Vector3 OffsetLocal = UpgradeButton.InverseTransformVector(OffsetWorld);
        UpgradeButton.anchoredPosition = UpgradeButton.anchoredPosition + (Vector2)OffsetLocal;
        UpgradeButton.gameObject.SetActive(bIsVisible);
    }

    private void ShowPinButtonAtCard(Card Card, bool bIsVisible)
    {
        if (Card == null)
        {
            HideCardButtons();
            return;
        }

        CardContainerUI Container = Card.GetCurrentCollection() as CardContainerUI;
        bool bIsActive = Container != null;
        bIsVisible = bIsVisible && bIsActive;

        RectTransform RectTransform = Card.GetComponent<RectTransform>();
        Vector3 TargetPosition = RectTransform.position;
        TargetPosition.x += 200 / 2f - 15;
        TargetPosition.y += 320 / 2f - 15;
        Vector3 OffsetWorld = TargetPosition - PinButton.position;
        Vector3 OffsetLocal = PinButton.InverseTransformVector(OffsetWorld);
        PinButton.GetComponent<Image>().sprite = Card.IsPinned() ? PinActive : PinInactive;
        PinButton.anchoredPosition = PinButton.anchoredPosition + (Vector2)OffsetLocal;
        PinButton.gameObject.SetActive(bIsVisible);
    }

    private void HideUpgradeButton()
    {
        UpgradeButton.gameObject.SetActive(false);
    }

    private void HidePinButton()
    {
        PinButton.gameObject.SetActive(false);
    }

    public void HideCardButtons()
    {
        HideUpgradeButton();
        HidePinButton();
    }

    public void LoadCard()
    {
        if (LastCard is not BuildingCard)
            return;
        if (!Game.TryGetService(out CardFactory CardFactory))
            return;

        CardFactory.CloneCard(LastCard, OnLoadedCard);
    }

    private void OnLoadedCard(Card Card)
    {
        BuildingCard LastBuildingCard = LastCard as BuildingCard;
        CopyCard = Card as BuildingCard;

        MoveToContainer(CopyCard.gameObject, false);
        ConvertToButton(LastBuildingCard.GetBuildingData(), CopyCard.GetMaxWorkerTransform(), UpgradeableAttributes.MaxWorker);
        ConvertToButton(LastBuildingCard.GetBuildingData(), CopyCard.GetUsagesTransform(), UpgradeableAttributes.MaxUsages);
        ConvertToButton(LastBuildingCard.GetBuildingData(), CopyCard.GetProductionTransform(), UpgradeableAttributes.Production);
        ConvertToButton(LastBuildingCard.GetBuildingData(), CopyCard.GetBuildableOnTransform(), UpgradeableAttributes.BuildableTiles);
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

    private void ConvertToButton(BuildingEntity BuildingData, Transform OldTransform, UpgradeableAttributes Type)
    {
        if (!BuildingData.IsUpgradePossible(Type))
            return;

        if (!Game.TryGetService(out IconFactory IconFactory))
            return;

        RectTransform UsageVisuals = OldTransform.GetChild(0).GetComponent<RectTransform>();

        GameObject Button = IconFactory.ConvertVisualsToButton(OldTransform, UsageVisuals);
        Button UsagesButton = Button.GetComponent<Button>();
        UsagesButton.onClick.RemoveAllListeners();
        UsagesButton.onClick.AddListener(delegate { SelectUpgrade(Type); });
    }

    private void EnableConfirmButton(bool bIsEnabled) { 
        ConfirmButton.GetComponent<Button>().interactable = bIsEnabled;
    }

    private void SelectUpgrade(UpgradeableAttributes SelectedUpgrade)
    {
        if (LastCard is not BuildingCard)
            return;
        if (!Game.TryGetService(out CardFactory CardFactory))
            return;

        BuildingCard LastBuildingCard = LastCard as BuildingCard;
        UpgradedBuildingData = Instantiate(LastBuildingCard.GetBuildingData());
        UpgradedBuildingData.Upgrade(SelectedUpgrade);
        UpgradedCardContainer.SetActive(true);
        UpgradeArrow.SetActive(true);
        ConfirmButton.SetActive(true);
        EnableConfirmButton(true);

        if (UpgradedCard != null)
        {
            Destroy(UpgradedCard.gameObject);
        }

        BuildingCardDTO DTO = new();
        DTO.BuildingData = UpgradedBuildingData;
        CardFactory.CreateCardFromDTO(DTO, 0, null, OnUpgradePreviewCard);
    }

    private void OnUpgradePreviewCard(Card Card)
    {
        UpgradedCard = Card as BuildingCard;
        MoveToContainer(UpgradedCard.gameObject, true);
    }

    public void ConfirmUpgrade()
    {
        if (LastCard is not BuildingCard)
            return;
        if (!Game.TryGetService(out Stockpile Stockpile))
            return;

        Destroy(CopyCard.gameObject);
        BuildingCard LastBuildingCard = LastCard as BuildingCard;
        LastBuildingCard.SetBuildingData(UpgradedBuildingData);
        LastCard.GenerateCard();
        Hide();
        Stockpile.AddUpgrades(-1);
        CardSelectionUI.UpdateText();
    }

    public void OnPinCard()
    {
        if (LastCard == null)
            return;

        bool bShouldBePinned = !LastCard.IsPinned();
        int Index = bShouldBePinned ? LastCard.GetIndex(true) : -1;
        LastCard.SetPinned(Index);
        ShowButtonsAtCard(LastCard, true);
    }

    protected override void StartServiceInternal() {}

    protected override void StopServiceInternal() {}
}
