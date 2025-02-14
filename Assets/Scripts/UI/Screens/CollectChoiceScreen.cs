using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

/** 
 * Screen class to share generating cards to chose from, eg for the merchant
 * Stores types in the public variables and creates choices to save access to cards
 */
public abstract class CollectChoiceScreen : ScreenUI
{

    public List<Transform> ChoiceContainers = new();
    public List<CollectableChoice.ChoiceType> ChoiceTypes = new();
    public SerializedDictionary<CollectableChoice.ChoiceType, GameObject> ChoicesPrefab = new();

    protected CollectableChoice[] Choices;
    protected bool bCloseOnPick = true;
    protected bool bDestroyOnPick = true;

    protected void Create()
    {
        Initialize();
        Choices = new CollectableChoice[ChoiceTypes.Count];
        CreateContainers();
        for (int i = 0; i < Choices.Length; i++)
        {
            CreateChoiceAt(i);
        }
    }

    private void CreateContainers()
    {
        if (!Game.TryGetService(out IconFactory IconFactory))
            return;

        ChoiceContainers = new(Choices.Length);
        for (int i = 0; i < Choices.Length; i++)
        {
            GameObject ContainerObj = IconFactory.GetChoice();
            var ContainerRect = ContainerObj.GetComponent<RectTransform>();
            ContainerRect.SetParent(Container.transform, false);
            ChoiceContainers.Add(ContainerRect);
            ContainerRect.anchoredPosition = new(GetPositionForContainer(i), ContainerRect.anchoredPosition.y);
            if (i == Choices.Length - 1)
                continue;

            int xMid = (GetPositionForContainer(i) + GetPositionForContainer(i + 1)) / 2;
            GameObject DividerObj = IconFactory.GetChoiceDivider();
            var DividerRect = DividerObj.GetComponent<RectTransform>();
            DividerRect.SetParent(Container.transform, false);
            DividerRect.anchoredPosition = new(xMid, DividerRect.anchoredPosition.y);
        }
    }

    private int GetPositionForContainer(int i)
    {
        int Count = Choices.Length;
        bool bIsEven = (Count % 2) == 0;
        int Half = bIsEven ? Count / 2 : (Count + 1) / 2;
        int Offset = bIsEven ? (int)Card.Width : (int)(Card.Width * 1.5);
        int x = bIsEven ? i : i + 1;
        x = (x - Half) * Offset + GetXOffsetBetweenChoices() * i;
        return x;
    }

    protected virtual void CreateChoiceAt(int i)
    {
        ChoiceContainers[i].gameObject.SetActive(true);

        // additional choices in subclasses
        switch (ChoiceTypes[i])
        {
            case CollectableChoice.ChoiceType.Building: CreateCardAt(i, GetCardTypeAt(i)); break;
        }
    }

    protected abstract CardDTO.Type GetCardTypeAt(int i);
    protected abstract bool ShouldCardBeUnlocked(int i);
    protected abstract Production GetCostsForChoice(int i);
    protected abstract int GetUpgradeCostsForChoice(int i);
    protected abstract int GetWorkerCostsForChoice(int i);
    protected abstract CardCollection GetTargetCardCollection();
    protected abstract int GetXOffsetBetweenChoices();
    protected abstract int GetSeed();

    protected void CreateCardAt(int ChoiceIndex, CardDTO.Type Type)
    {
        switch (Type)
        {
            case CardDTO.Type.Building: CreateBuildingCardAt(ChoiceIndex); break;
            case CardDTO.Type.Event: CreateEventCardAt(ChoiceIndex); break;
        }
    }

    private void CreateEventCardAt(int ChoiceIndex)
    {
        if (!Game.TryGetService(out CardFactory CardFactory))
            return;

        PrepareContainerForCard(ChoiceIndex, out Transform CardContainer, out Action<int, Card> Callback);
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

        PrepareContainerForCard(ChoiceIndex, out Transform CardContainer, out var Callback);
        CardFactory.CreateCard(TargetBuilding, ChoiceIndex, CardContainer, Callback);
    }

    protected virtual void PrepareContainerForCard(int ChoiceIndex, out Transform CardContainer, out Action<int, Card> Callback)
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
        Callback = SetChoiceBuilding;
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

    protected bool CanAffordChoice(int ChoiceIndex)
    {
        // cant enfore having workers, as they do not exist in CardSelection (but we still need unlocking)
        Stockpile Stockpile = Game.GetService<Stockpile>();
        Workers Workers = Game.GetService<Workers>();

        GetCostsForChoice(ChoiceIndex, out var Costs, out var UpgradeCosts, out var WorkerCosts);

        bool bCanPayStockpile = Stockpile != null && Stockpile.CanAfford(Costs) && Stockpile.CanAffordUpgrade(UpgradeCosts);
        bool bStockpileFree = (UpgradeCosts + Costs.SumUp()) == 0;

        bool bCanPayWorkers = Workers != null && Workers.GetTotalWorkerCount() >= WorkerCosts;
        bool bWorkersFree = WorkerCosts == 0;

        return (bStockpileFree || bCanPayStockpile) && (bWorkersFree || bCanPayWorkers);
    }

    protected void GetCostsForChoice(int ChoiceIndex, out Production Costs, out int UpgradeCosts, out int WorkerCosts)
    {
        Costs = GetCostsForChoice(ChoiceIndex);
        UpgradeCosts = GetUpgradeCostsForChoice(ChoiceIndex);
        WorkerCosts = GetWorkerCostsForChoice(ChoiceIndex);
    }

    protected void AddPrefabToContainer(Transform Container, GameObject Prefab)
    {
        if (Container.childCount > 0)
        {
            DestroyImmediate(Container.GetChild(0).gameObject);
        }

        Instantiate(Prefab, Container);
    }

    protected virtual void SetChoiceBuilding(int Index, Card Card)
    {
        Choices[Index] = new CollectableBuildingChoice(Card);
    }

    public virtual void OnSelectOption(int ChoiceIndex)
    {
        Stockpile Stockpile = Game.GetService<Stockpile>();
        Workers Workers = Game.GetService<Workers>();

        if (!CanAffordChoice(ChoiceIndex))
            return;

        GetCostsForChoice(ChoiceIndex, out var Costs, out var UpgradeCosts, out var WorkerCosts);
        if (Stockpile != null)
        {
            Stockpile.Pay(Costs);
            Stockpile.PayUpgrade(UpgradeCosts);
        }
        if (Workers != null)
        {
            Workers.KillWorkers(WorkerCosts);
        }

        CollectableChoice Choice = Choices[ChoiceIndex];
        List<CollectableChoice> OtherChoices = new();
        OtherChoices.AddRange(Choices);
        OtherChoices.Remove(Choice);

        switch (Choice.Type)
        {
            case CollectableChoice.ChoiceType.Building: OnSelectBuildingChoice(Choice); break;
            case CollectableChoice.ChoiceType.Upgrade: OnSelectUpgradeChoice(Choice); break;
            case CollectableChoice.ChoiceType.Relic: OnSelectRelicChoice(Choice); break;
            case CollectableChoice.ChoiceType.Offering: // intentional fallthrough
            case CollectableChoice.ChoiceType.Sacrifice: OnSelectAltarChoice(Choice); break;
            case CollectableChoice.ChoiceType.Amber: OnSelectAmberChoice(Choice); break;
            case CollectableChoice.ChoiceType.Locked: throw new Exception("Should never reach here - not a valid choice");
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

    private void OnSelectBuildingChoice(CollectableChoice Choice)
    {
        CollectableBuildingChoice BuildingChoice = Choice as CollectableBuildingChoice;
        if (BuildingChoice == null)
            return;

        if (!Game.TryGetService(out BuildingService BuildingService))
            return;

        CardCollection Collection = GetTargetCardCollection();
        if (Collection == null)
            return;

        Collection.AddCard(BuildingChoice.GeneratedCard);

        if (BuildingChoice.BuildingToUnlock == BuildingConfig.Type.DEFAULT)
            return;

        BuildingService.UnlockableBuildings[BuildingChoice.BuildingToUnlock] = Unlockables.State.Unlocked;
    }

    private void OnSelectRelicChoice(CollectableChoice Choice)
    {
        CollectableRelicChoice RelicChoice = Choice as CollectableRelicChoice;
        if (RelicChoice == null)
            return;

        if (!Game.TryGetService(out RelicService RelicService))
            return;

        if (RelicChoice.RelicToUnlock == RelicType.DEFAULT)
            return;

        RelicService.UnlockableRelics[RelicChoice.RelicToUnlock] = Unlockables.State.Unlocked;
    }

    private void OnSelectUpgradeChoice(CollectableChoice Choice)
    {
        if (!Game.TryGetService(out Stockpile Stockpile))
            return;

        Stockpile.AddUpgrades(2);
    }

    protected virtual void OnSelectAltarChoice(CollectableChoice Choice)
    {
        // overridden in subclasses
    }

    protected virtual void OnSelectAmberChoice(CollectableChoice Choice)
    {
        // overridden in subclasses
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

        if (Choice.Type != CollectableChoice.ChoiceType.Building)
            return;

        CollectableBuildingChoice BuildingChoice = Choice as CollectableBuildingChoice;
        if (BuildingChoice == null)
            return;

        Destroy(BuildingChoice.GeneratedCard.gameObject);
    }


}
