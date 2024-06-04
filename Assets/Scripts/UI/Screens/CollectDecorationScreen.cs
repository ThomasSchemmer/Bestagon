using System;
using UnityEngine;
using UnityEngine.UI;

/** 
 * Screen showing the player either a choice between two event cards (tribe) or 
 * between a new building and 2 upgrade points (ruins)
 */
public class CollectDecorationScreen : MonoBehaviour
{
    private class DecorationChoice
    {
        public bool bIsCardChoice;
        public BuildingConfig.Type BuildingToUnlock = BuildingConfig.Type.DEFAULT;
        public Card GeneratedCard;

        public DecorationChoice(Card GeneratedCard)
        {
            this.bIsCardChoice = true;
            this.GeneratedCard = GeneratedCard;
            if (GeneratedCard is not BuildingCard)
                return;

            BuildingCard Building = GeneratedCard as BuildingCard;
            BuildingToUnlock = Building.GetBuildingData().BuildingType;
        }

        public DecorationChoice()
        {
            bIsCardChoice = false;
        }
    }

    public Transform ContainerOptionA, ContainerOptionB;
    public GameObject GeneratedCardPrefab, GeneratedUpgradePrefab;

    private DecorationChoice ChoiceA, ChoiceB;
    private GameObject Container;
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

    public void OnSelectOption(bool bIsChoiceA)
    {
        DecorationChoice Choice = bIsChoiceA ? ChoiceA : ChoiceB;
        DecorationChoice OtherChoice = bIsChoiceA ? ChoiceB : ChoiceA;

        switch (Choice.bIsCardChoice)
        {
            case true: OnSelectCardChoice(Choice); break;
            case false: OnSelectUpgradeChoice(Choice); break;
        }

        DestroyChoice(OtherChoice);

        RemoveDecoration();
        Hide();
        Deselect();
    }

    private void Deselect()
    {
        if (!Game.TryGetService(out Selectors Selectors))
            return;

        Selectors.ForceDeselect();
    }

    private void OnSelectCardChoice(DecorationChoice Choice)
    {
        if (!Game.TryGetServices(out Unlockables Unlockables, out CardHand CardHand))
            return;

        CardHand.AddCard(Choice.GeneratedCard);

        if (Choice.BuildingToUnlock == BuildingConfig.Type.DEFAULT)
            return;

        Unlockables.UnlockSpecificBuildingType(Choice.BuildingToUnlock);
    }

    private void OnSelectUpgradeChoice(DecorationChoice Choice)
    {
        if (!Game.TryGetService(out Stockpile Stockpile))
            return;

        Stockpile.UpgradePoints += 2;
    }

    private void DestroyChoice(DecorationChoice Choice)
    {
        // updates dont have any created card objects
        if (!Choice.bIsCardChoice)
            return;

        Destroy(Choice.GeneratedCard.gameObject);
    }

    private void RemoveDecoration()
    {
        if (!Game.TryGetService(out MapGenerator MapGenerator))
            return;

        if (!MapGenerator.TryGetHexagonData(CurrentLocation, out HexagonData HexData))
            return;

        HexData.Decoration = HexagonConfig.HexagonDecoration.None;
        if (!MapGenerator.TryGetHexagon(CurrentLocation, out HexagonVisualization HexVis))
            return;

        HexVis.UpdateMesh();
    }

    public void ShowRuinChoices()
    {
        CreateCardAt(true, CardDTO.Type.Building);
        CreateUpgradeAt(false);
    }

    public void ShowTribeChoices()
    {
        CreateCardAt(true, CardDTO.Type.Event);
        CreateCardAt(false, CardDTO.Type.Event);
    }

    private void CreateUpgradeAt(bool bIsChoiceA)
    {
        // keep the kinda clunky callback syntax to be similar to the CreateCardAt function
        PrepareContainerForUpgrade(bIsChoiceA, out Action Callback);
        Callback();
    }

    private void CreateCardAt(bool bIsChoiceA, CardDTO.Type Type) { 
        switch (Type)
        {
            case CardDTO.Type.Building: CreateBuildingCardAt(bIsChoiceA); break;
            case CardDTO.Type.Event: CreateEventCardAt(bIsChoiceA); break;
        }   
    }

    private void CreateEventCardAt(bool bIsChoiceA)
    {
        if (!Game.TryGetServices(out Unlockables Unlockables, out CardFactory CardFactory))
            return;

        PrepareContainerForCard(bIsChoiceA, out Transform CardContainer, out Action<Card> Callback);
        CardFactory.CreateCard(EventData.GetRandomType(), 0, CardContainer, Callback);
    }

    private void CreateBuildingCardAt(bool bIsChoiceA)
    {
        if (!Game.TryGetServices(out Unlockables Unlockables, out CardFactory CardFactory))
            return;

        // preview cause we dont wanna unlock it just yet - wait for the actual choice
        if (!Unlockables.TryUnlockNewBuildingType(out BuildingConfig.Type BuildingToUnlock, true))
            return;

        PrepareContainerForCard(bIsChoiceA, out Transform CardContainer, out Action<Card> Callback);
        CardFactory.CreateCard(BuildingToUnlock, 0, CardContainer, Callback);
    }

    private void PrepareContainerForCard(bool bIsChoiceA, out Transform CardContainer, out Action<Card> Callback)
    {
        Transform TargetContainer = bIsChoiceA ? ContainerOptionA : ContainerOptionB;
        AddPrefabToContainer(TargetContainer, GeneratedCardPrefab);
        Button Button = TargetContainer.GetChild(0).GetChild(2).GetComponent<Button>();
        Button.onClick.RemoveAllListeners();
        Button.onClick.AddListener(() =>
        {
            OnSelectOption(bIsChoiceA);
        });
        CardContainer = TargetContainer.GetChild(0).GetChild(3);
        Callback = bIsChoiceA ? SetChoiceACard : SetChoiceBCard;
    }

    private void PrepareContainerForUpgrade(bool bIsChoiceA, out Action Callback) {
        Transform TargetContainer = bIsChoiceA ? ContainerOptionA : ContainerOptionB;
        AddPrefabToContainer(TargetContainer, GeneratedUpgradePrefab);
        Button Button = TargetContainer.GetChild(0).GetChild(2).GetComponent<Button>();
        Button.onClick.RemoveAllListeners();
        Button.onClick.AddListener(() =>
        {
            OnSelectOption(bIsChoiceA);
        });
        Callback = bIsChoiceA ? SetChoiceAUpgrade : SetChoiceBUpgrade;
    }

    private void AddPrefabToContainer(Transform Container, GameObject Prefab)
    {
        if (Container.childCount > 0)
        {
            DestroyImmediate(Container.GetChild(0).gameObject);
        }

        Instantiate(Prefab, Container);
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

        Game.Instance.OnPopupAction(true);
        Container.SetActive(true);

        switch (HexData.Decoration)
        {
            case HexagonConfig.HexagonDecoration.Ruins: ShowRuinChoices(); break;
            case HexagonConfig.HexagonDecoration.Tribe: ShowTribeChoices(); break;
        }
    }

    private void SetChoiceACard(Card Card)
    {
        ChoiceA = new(Card);
    }

    private void SetChoiceAUpgrade()
    {
        ChoiceA = new();
    }

    private void SetChoiceBUpgrade()
    {
        ChoiceB = new();
    }

    private void SetChoiceBCard(Card Card) { 
        ChoiceB = new(Card); 
    }

    public void Hide()
    {
        Game.Instance.OnPopupAction(false);
        Container.SetActive(false);
    }
}
