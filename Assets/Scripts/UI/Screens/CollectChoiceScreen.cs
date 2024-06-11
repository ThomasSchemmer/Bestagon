using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/** 
 * Screen class to share generating cards to chose from, eg for the merchant
 * Stores types in the public variables and creates choices to save access to cards
 */
public abstract class CollectChoiceScreen : MonoBehaviour
{
    public enum ChoiceType
    {
        Card,
        Upgrade
    }

    public class CollectableChoice
    {
        public ChoiceType Type;
        public BuildingConfig.Type BuildingToUnlock = BuildingConfig.Type.DEFAULT;
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
    }

    public List<Transform> ChoiceContainers = new();
    public List<ChoiceType> ChoiceTypes = new();
    public SerializedDictionary<ChoiceType, GameObject> ChoicesPrefab = new();

    protected CollectableChoice[] Choices;
    protected GameObject Container;

    protected void Create()
    {
        Choices = new CollectableChoice[ChoiceContainers.Count];
        for (int i = 0; i < ChoiceContainers.Count; i++) { 
            switch (ChoiceTypes[i])
            {
                case ChoiceType.Card: CreateCardAt(i, GetCardTypeAt(i)); break;
                case ChoiceType.Upgrade: CreateUpgradeAt(i); break;
            }
        }
    }

    protected abstract CardDTO.Type GetCardTypeAt(int i);
    protected abstract bool ShouldCardBeUnlocked(int i);

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
        PrepareContainerForUpgrade(ChoiceIndex, out Action<Card, int> Callback);
        Callback(null, ChoiceIndex);
    }

    private void CreateEventCardAt(int ChoiceIndex)
    {
        if (!Game.TryGetService(out CardFactory CardFactory))
            return;

        PrepareContainerForCard(ChoiceIndex, out Transform CardContainer, out Action<Card, int> Callback);
        CardFactory.CreateCard(EventData.GetRandomType(), ChoiceIndex, CardContainer, Callback);
    }

    private void CreateBuildingCardAt(int ChoiceIndex)
    {
        if (!Game.TryGetServices(out Unlockables Unlockables, out CardFactory CardFactory))
            return;

        BuildingConfig.Type BuildingToUnlock;
        if (ShouldCardBeUnlocked(ChoiceIndex))
        {
            // preview cause we dont wanna unlock it just yet - wait for the actual choice
            if (!Unlockables.TryUnlockNewBuildingType(out BuildingToUnlock, true))
                return;
        }
        else
        {
            BuildingToUnlock = Unlockables.GetRandomUnlockedType();
        }

        PrepareContainerForCard(ChoiceIndex, out Transform CardContainer, out Action<Card, int> Callback);
        CardFactory.CreateCard(BuildingToUnlock, ChoiceIndex, CardContainer, Callback);
    }


    private void PrepareContainerForCard(int ChoiceIndex, out Transform CardContainer, out Action<Card, int> Callback)
    {
        Transform TargetContainer = ChoiceContainers[ChoiceIndex];
        AddPrefabToContainer(TargetContainer, ChoicesPrefab[ChoiceTypes[ChoiceIndex]]);
        Button Button = TargetContainer.GetChild(0).GetChild(2).GetComponent<Button>();
        Button.onClick.RemoveAllListeners();
        Button.onClick.AddListener(() =>
        {
            OnSelectOption(ChoiceIndex);
        });
        CardContainer = TargetContainer.GetChild(0).GetChild(3);
        Callback = SetChoiceCard;
    }

    private void PrepareContainerForUpgrade(int ChoiceIndex, out Action<Card, int> Callback)
    {
        Transform TargetContainer = ChoiceContainers[ChoiceIndex];
        AddPrefabToContainer(TargetContainer, ChoicesPrefab[ChoiceTypes[ChoiceIndex]]);
        Button Button = TargetContainer.GetChild(0).GetChild(2).GetComponent<Button>();
        Button.onClick.RemoveAllListeners();
        Button.onClick.AddListener(() =>
        {
            OnSelectOption(ChoiceIndex);
        });
        Callback = SetChoiceUpgrade;
    }

    private void AddPrefabToContainer(Transform Container, GameObject Prefab)
    {
        if (Container.childCount > 0)
        {
            DestroyImmediate(Container.GetChild(0).gameObject);
        }

        Instantiate(Prefab, Container);
    }

    private void SetChoiceCard(Card Card, int i)
    {
        Choices[i] = new(Card);
    }

    private void SetChoiceUpgrade(Card Card, int i)
    {
        // dont need to save anything, the upgrade definition is implicit
        Choices[i] = new();
    }

    public virtual void OnSelectOption(int ChoiceIndex)
    {
        CollectableChoice Choice = Choices[ChoiceIndex];
        List<CollectableChoice> OtherChoices = new();
        OtherChoices.AddRange(Choices);
        OtherChoices.Remove(Choice);

        switch (Choice.Type)
        {
            case ChoiceType.Card: OnSelectCardChoice(Choice); break;
            case ChoiceType.Upgrade: OnSelectUpgradeChoice(Choice); break;
        }

        for (int i = OtherChoices.Count; i > 0; i--)
        {
            DestroyChoice(OtherChoices[i - 1]);
        }
        
        Deselect();
    }

    private void OnSelectCardChoice(CollectableChoice Choice)
    {
        if (!Game.TryGetServices(out Unlockables Unlockables, out CardHand CardHand))
            return;

        CardHand.AddCard(Choice.GeneratedCard);

        if (Choice.BuildingToUnlock == BuildingConfig.Type.DEFAULT)
            return;

        Unlockables.UnlockSpecificBuildingType(Choice.BuildingToUnlock);
    }

    private void OnSelectUpgradeChoice(CollectableChoice Choice)
    {
        if (!Game.TryGetService(out Stockpile Stockpile))
            return;

        Stockpile.UpgradePoints += 2;
    }

    private void Deselect()
    {
        if (!Game.TryGetService(out Selectors Selectors))
            return;

        Selectors.ForceDeselect();
    }

    private void DestroyChoice(CollectableChoice Choice)
    {
        // updates dont have any created card objects
        if (Choice.Type != ChoiceType.Card)
            return;

        Destroy(Choice.GeneratedCard.gameObject);
    }

    protected virtual void Show()
    {
        Game.Instance.OnPopupAction(true);
        Container.SetActive(true);
    }

    protected virtual void Close()
    {
        Game.Instance.OnPopupAction(false);
        Container.SetActive(false);
    }


}
