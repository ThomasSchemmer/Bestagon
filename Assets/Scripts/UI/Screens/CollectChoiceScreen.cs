using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/** 
 * Screen class to share generating cards to chose from, eg for the merchant
 * Stores types in the public variables and creates choices to save access to cards
 */
public abstract class CollectChoiceScreen : ScreenUI
{
    public enum ChoiceType
    {
        Card,
        Upgrade,
        Relic
    }

    public class CollectableChoice
    {
        public ChoiceType Type;
        public BuildingConfig.Type BuildingToUnlock = BuildingConfig.Type.DEFAULT;
        public RelicType RelicToUnlock = RelicType.DEFAULT;
        public Card GeneratedCard;

        public CollectableChoice(Card GeneratedCard)
        {
            this.Type = ChoiceType.Card;
            this.GeneratedCard = GeneratedCard;
            if (GeneratedCard is not BuildingCard)
                return;

            BuildingCard Building = GeneratedCard as BuildingCard;
            BuildingToUnlock = Building.GetBuildingData().BuildingType;
        }

        public CollectableChoice()
        {
            Type = ChoiceType.Upgrade;
        }

        public CollectableChoice(RelicType RelicType)
        {
            RelicToUnlock = RelicType;
            Type = ChoiceType.Relic;
        }
    }

    public List<Transform> ChoiceContainers = new();
    public List<ChoiceType> ChoiceTypes = new();
    public SerializedDictionary<ChoiceType, GameObject> ChoicesPrefab = new();

    protected CollectableChoice[] Choices;
    protected bool bCloseOnPick = true;
    protected bool bDestroyOnPick = true;

    protected void Create()
    {
        Choices = new CollectableChoice[ChoiceContainers.Count];
        for (int i = 0; i < ChoiceContainers.Count; i++)
        {
            ChoiceContainers[i].gameObject.SetActive(true);

            switch (ChoiceTypes[i])
            {
                case ChoiceType.Card: CreateCardAt(i, GetCardTypeAt(i)); break;
                case ChoiceType.Upgrade: CreateUpgradeAt(i); break;
                case ChoiceType.Relic: CreateRelicAt(i); break;
            }
        }
    }

    protected abstract CardDTO.Type GetCardTypeAt(int i);
    protected abstract bool ShouldCardBeUnlocked(int i);
    protected abstract Production GetCostsForChoice(int i);
    protected abstract int GetUpgradeCostsForChoice(int i);
    protected abstract CardCollection GetTargetCardCollection();
    protected abstract int GetSeed();

    protected void CreateCardAt(int ChoiceIndex, CardDTO.Type Type)
    {
        switch (Type)
        {
            case CardDTO.Type.Building: CreateBuildingCardAt(ChoiceIndex); break;
            case CardDTO.Type.Event: CreateEventCardAt(ChoiceIndex); break;
        }
    }

    protected void CreateUpgradeAt(int ChoiceIndex)
    {
        // keep the kinda clunky callback syntax to be similar to the CreateCardAt function
        PrepareContainerForUpgrade(ChoiceIndex, out Action<Card, RelicType, int> Callback);
        Callback(null, RelicType.DEFAULT, ChoiceIndex);
    }

    protected void CreateRelicAt(int ChoiceIndex)
    {
        if (!Game.TryGetService(out RelicService RelicService))
            return;

        if (!RelicService.UnlockableRelics.TryUnlockNewType(GetSeed() + ChoiceIndex, out var RelicType, true))
            return;

        PrepareContainerForRelic(ChoiceIndex, out Transform RelicContainer, out Action<Card, RelicType, int> Callback);
        RelicService.CreateRelicIcon(RelicContainer, RelicService.Relics[RelicType], true);
        Callback(null, RelicType, ChoiceIndex);
    }

    private void CreateEventCardAt(int ChoiceIndex)
    {
        if (!Game.TryGetService(out CardFactory CardFactory))
            return;

        PrepareContainerForCard(ChoiceIndex, out Transform CardContainer, out Action<Card, RelicType, int> Callback);
        CardFactory.CreateCard(EventData.GetRandomType(GetSeed() + ChoiceIndex), ChoiceIndex, CardContainer, Callback);
    }

    private void CreateBuildingCardAt(int ChoiceIndex)
    {
        if (!Game.TryGetServices(out BuildingService BuildingService, out CardFactory CardFactory))
            return;

        BuildingConfig.Type TargetBuilding;
        if (ShouldCardBeUnlocked(ChoiceIndex))
        {
            // preview cause we dont wanna unlock it just yet - wait for the actual choice
            if (!BuildingService.UnlockableBuildings.TryUnlockNewType(GetSeed() + ChoiceIndex, out TargetBuilding, true))
                return;
        }
        else
        {
            // just get a random, already unlocked one to duplicate
            TargetBuilding = BuildingService.UnlockableBuildings.GetRandomOfState(GetSeed() + ChoiceIndex, Unlockables.State.Unlocked, true, false);
        }

        PrepareContainerForCard(ChoiceIndex, out Transform CardContainer, out Action<Card, RelicType, int> Callback);
        CardFactory.CreateCard(TargetBuilding, ChoiceIndex, CardContainer, Callback);
    }


    protected virtual void PrepareContainerForCard(int ChoiceIndex, out Transform CardContainer, out Action<Card, RelicType, int> Callback)
    {
        Transform TargetContainer = ChoiceContainers[ChoiceIndex];
        AddPrefabToContainer(TargetContainer, ChoicesPrefab[ChoiceTypes[ChoiceIndex]]);
        Button Button = TargetContainer.GetChild(0).GetChild(2).GetComponent<Button>();
        Button.onClick.RemoveAllListeners();
        Button.onClick.AddListener(() =>
        {
            OnSelectOption(ChoiceIndex);
        });
        Button.interactable = CanAffordChoice(ChoiceIndex);
        CardContainer = TargetContainer.GetChild(0).GetChild(3);
        Callback = SetChoiceCard;
    }

    private void PrepareContainerForUpgrade(int ChoiceIndex, out Action<Card, RelicType, int> Callback)
    {
        Transform TargetContainer = ChoiceContainers[ChoiceIndex];
        AddPrefabToContainer(TargetContainer, ChoicesPrefab[ChoiceTypes[ChoiceIndex]]);
        Button Button = TargetContainer.GetChild(0).GetChild(2).GetComponent<Button>();
        Button.onClick.RemoveAllListeners();
        Button.onClick.AddListener(() =>
        {
            OnSelectOption(ChoiceIndex);
        });
        Button.interactable = CanAffordChoice(ChoiceIndex);
        Callback = SetChoiceUpgrade;
    }

    private void PrepareContainerForRelic(int ChoiceIndex, out Transform RelicContainer, out Action<Card, RelicType, int> Callback)
    {
        Transform TargetContainer = ChoiceContainers[ChoiceIndex];
        AddPrefabToContainer(TargetContainer, ChoicesPrefab[ChoiceTypes[ChoiceIndex]]);
        Button Button = TargetContainer.GetChild(0).GetChild(2).GetComponent<Button>();
        Button.onClick.RemoveAllListeners();
        Button.onClick.AddListener(() =>
        {
            OnSelectOption(ChoiceIndex);
        });
        Button.interactable = CanAffordChoice(ChoiceIndex);
        RelicContainer = TargetContainer.GetChild(0).GetChild(3);
        Callback = SetChoiceRelic;
    }

    public void UpdateSelectChoiceButtons()
    {
        for (int i = 0; i < ChoiceContainers.Count; i++)
        {
            if (ChoiceContainers[i] == null)
                continue;

            Button Button = ChoiceContainers[i].GetChild(0).GetChild(2).GetComponent<Button>();
            if (Button == null)
                continue;

            Button.interactable = CanAffordChoice(i);
        }
    }

    private bool CanAffordChoice(int ChoiceIndex)
    {
        if (!Game.TryGetService(out Stockpile Stockpile))
            return false;

        Production Costs = GetCostsForChoice(ChoiceIndex);
        int UpgradeCosts = GetUpgradeCostsForChoice(ChoiceIndex);
        return Stockpile.CanAfford(Costs) && Stockpile.CanAffordUpgrade(UpgradeCosts);
    }

    private void AddPrefabToContainer(Transform Container, GameObject Prefab)
    {
        if (Container.childCount > 0)
        {
            DestroyImmediate(Container.GetChild(0).gameObject);
        }

        Instantiate(Prefab, Container);
    }

    protected virtual void SetChoiceCard(Card Card, RelicType RelicType, int i)
    {
        Choices[i] = new(Card);
    }

    protected virtual void SetChoiceUpgrade(Card Card, RelicType RelicType, int i)
    {
        // dont need to save anything, the upgrade definition is implicit
        Choices[i] = new();
    }

    protected virtual void SetChoiceRelic(Card Card, RelicType RelicType, int i)
    {
        Choices[i] = new(RelicType);
    }

    public virtual void OnSelectOption(int ChoiceIndex)
    {
        if (!Game.TryGetService(out Stockpile Stockpile))
            return;

        Production Costs = GetCostsForChoice(ChoiceIndex);
        int UpgradeCosts = GetUpgradeCostsForChoice(ChoiceIndex);
        if (!Stockpile.CanAfford(Costs) ||!Stockpile.CanAffordUpgrade(UpgradeCosts))
            return;

        Stockpile.Pay(Costs);
        Stockpile.PayUpgrade(UpgradeCosts);

        CollectableChoice Choice = Choices[ChoiceIndex];
        List<CollectableChoice> OtherChoices = new();
        OtherChoices.AddRange(Choices);
        OtherChoices.Remove(Choice);

        switch (Choice.Type)
        {
            case ChoiceType.Card: OnSelectCardChoice(Choice); break;
            case ChoiceType.Upgrade: OnSelectUpgradeChoice(Choice); break;
            case ChoiceType.Relic: OnSelectRelicChoice(Choice); break;
        }
        DeactivateChoice(ChoiceIndex);
        Choices[ChoiceIndex] = null;

        if (bDestroyOnPick)
        {
            for (int i = OtherChoices.Count; i > 0; i--)
            {
                DestroyChoice(OtherChoices[i - 1]);
            }
            Choices = null;
        }

        if (bCloseOnPick)
        {
            Deselect();
        }
    }

    private void DeactivateChoice(int ChoiceIndex)
    {
        ChoiceContainers[ChoiceIndex].gameObject.SetActive(false);
    }

    private void OnSelectCardChoice(CollectableChoice Choice)
    {
        if (!Game.TryGetService(out BuildingService BuildingService))
            return;

        CardCollection Collection = GetTargetCardCollection();
        if (Collection == null)
            return;

        Collection.AddCard(Choice.GeneratedCard);

        if (Choice.BuildingToUnlock == BuildingConfig.Type.DEFAULT)
            return;

        BuildingService.UnlockableBuildings[Choice.BuildingToUnlock] = Unlockables.State.Unlocked;
    }

    private void OnSelectRelicChoice(CollectableChoice Choice)
    {
        if (!Game.TryGetService(out RelicService RelicService))
            return;

        if (Choice.RelicToUnlock == RelicType.DEFAULT)
            return;

        RelicService.UnlockableRelics[Choice.RelicToUnlock] = Unlockables.State.Unlocked;
    }

    private void OnSelectUpgradeChoice(CollectableChoice Choice)
    {
        if (!Game.TryGetService(out Stockpile Stockpile))
            return;

        Stockpile.AddUpgrades(2);
    }


    private void Deselect()
    {
        if (!Game.TryGetService(out Selectors Selectors))
            return;

        Selectors.ForceDeselect();
    }

    protected void DestroyChoice(CollectableChoice Choice)
    {
        if (Choice == null)
            return;

        // updates dont have any created card objects
        // TODO: fix for relic
        if (Choice.Type != ChoiceType.Card)
            return;

        Destroy(Choice.GeneratedCard.gameObject);
    }


}
