using System;
using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.UI;

/** 
 * Screen showing the player either a choice between two event cards (tribe) or 
 * between a new building and 2 upgrade points (ruins)
 */
public class CollectDecorationScreen : CollectChoiceScreen
{
    private Location CurrentLocation;

    private void Start()
    {
        TokenizedUnitEntity._OnMovementTo += HandleMovement;
        Container = transform.GetChild(0).gameObject;
        Hide();
    }

    private void OnDestroy()
    {
        TokenizedUnitEntity._OnMovementTo -= HandleMovement;
    }

    public override void OnSelectOption(int ChoiceIndex)
    {
        base.OnSelectOption(ChoiceIndex);
        Hide();

        if (!Game.TryGetServices(out MapGenerator MapGenerator, out DecorationService DecorationService))
            return;

        if (!MapGenerator.TryGetHexagonData(CurrentLocation, out HexagonData HexData))
            return;

        if (!DecorationService.TryGetEntityAt(CurrentLocation, out var Decoration))
            return;

        if (Decoration.ShouldBeKilledOnCollection())
        {
            DecorationService.KillEntity(Decoration);
        }

        if (!MapGenerator.TryGetChunkVis(CurrentLocation, out var ChunkVis))
            return;

        ChunkVis.RefreshTokens();
    }

    protected override void CreateChoiceAt(int i)
    {
        base.CreateChoiceAt(i);

        switch (ChoiceTypes[i])
        {
            case CollectableChoice.ChoiceType.Upgrade: CreateUpgradeAt(i); break;
            case CollectableChoice.ChoiceType.Relic: CreateRelicAt(i); break;
            case CollectableChoice.ChoiceType.Offering: // intentional fallthrough
            case CollectableChoice.ChoiceType.Sacrifice: CreateAltarAt(i); break;
        }
    }

    private void HandleMovement(Location Location)
    {
        if (!Game.TryGetService(out DecorationService DecSer))
            return;

        if (!DecSer.TryGetEntityAt(Location, out DecorationEntity Decoration))
            return;

        if (Decoration.IsActivated())
            return;

        CurrentLocation = Location;

        Show();

        switch (Decoration.DecorationType)
        {
            case DecorationEntity.DType.Ruins: ShowRuinChoices(); break;
            case DecorationEntity.DType.Tribe: ShowTribeChoices(); break;
            case DecorationEntity.DType.Treasure: ShowTreasureChoices(); break;
            case DecorationEntity.DType.Altar: ShowAltarChoices(); break;
        }
    }

    private void ShowRuinChoices()
    {
        ChoiceTypes = CanSpawnUpgradeRuins() ?
            new() { CollectableChoice.ChoiceType.Building, CollectableChoice.ChoiceType.Upgrade } :
            new() { CollectableChoice.ChoiceType.Upgrade };
        
        Create();
    }

    private void ShowTreasureChoices()
    {
        ChoiceTypes = new() { CollectableChoice.ChoiceType.Relic, CollectableChoice.ChoiceType.Relic };
        Create();
    }
    private void ShowAltarChoices()
    {
        ChoiceTypes = new() { CollectableChoice.ChoiceType.Offering, CollectableChoice.ChoiceType.Sacrifice };
        Create();
    }

    private void ShowTribeChoices()
    {
        ChoiceTypes = new() { CollectableChoice.ChoiceType.Building, CollectableChoice.ChoiceType.Building };
        Create();
    }

    protected override void OnSelectAltarChoice(CollectableChoice Choice)
    {
        base.OnSelectAltarChoice(Choice);
        CollectableAltarChoice AltarChoice = Choice as CollectableAltarChoice;
        if (AltarChoice == null)
            return;

        if (!Game.TryGetService(out DecorationService Decorations))
            return;

        if (!Decorations.TryGetEntityAt(CurrentLocation, out DecorationEntity Decoration))
            return;

        Decoration.ApplyEffect(AltarChoice.Type);
    }

    protected override CardDTO.Type GetCardTypeAt(int i)
    {
        switch (GetDecorationType())
        {
            case DecorationEntity.DType.Ruins: return CardDTO.Type.Building;
            case DecorationEntity.DType.Tribe: return CardDTO.Type.Event;
            default: return CardDTO.Type.Event;
        }
    }

    private DecorationEntity.DType GetDecorationType()
    {
        if (!Game.TryGetService(out DecorationService DecorationService))
            return default;

        if (!DecorationService.TryGetEntityAt(CurrentLocation, out DecorationEntity Entity))
            return default;

        return Entity.DecorationType;
    }

    protected override bool ShouldCardBeUnlocked(int i)
    {
        return GetDecorationType() == DecorationEntity.DType.Ruins;
    }

    protected override Production GetCostsForChoice(int i)
    {
        if (ChoiceTypes == null)
            return Production.Empty;
        if (ChoiceTypes[i] != CollectableChoice.ChoiceType.Offering && 
            ChoiceTypes[i] != CollectableChoice.ChoiceType.Sacrifice)
            return Production.Empty;

        return GetAltarChoiceCost(i);
    }

    protected override int GetUpgradeCostsForChoice(int i)
    {
        return 0;
    }
    protected override int GetWorkerCostsForChoice(int i)
    {
        if (ChoiceTypes[i] != CollectableChoice.ChoiceType.Offering &&
            ChoiceTypes[i] != CollectableChoice.ChoiceType.Sacrifice)
            return 0;

        if (i == 0)
            return 0;

        return 1;
    }

    protected override CardCollection GetTargetCardCollection()
    {
        return Game.GetService<CardHand>();
    }

    protected override int GetSeed()
    {
        return CurrentLocation.GetHashCode();
    }

    private Production GetAltarChoiceCost(int Index)
    {
        if (!Game.TryGetService(out BuildingService Buildings))
            return Production.Empty;

        if (Index != 0)
            return Production.Empty;

        int Minimum = 1;
        int Maximum = 4;
        Unlockables.State TargetState = Unlockables.State.Unlocked;

        int Seed = CurrentLocation.GetHashCode();
        bool bSuccess = Buildings.TryGetRandomResource(Seed, TargetState, false, out var Type1);
        Seed += 1;
        bSuccess &= Buildings.TryGetRandomResource(Seed, TargetState, false, out var Type2);
        Seed += 1;
        bSuccess &= Buildings.TryGetRandomResource(Seed, TargetState, false, out var Type3);
        if (!bSuccess)
            return Production.Empty;

        // its fine to have multiple times the same, cost still goes up
        Production Result = new Production();
        Result += new Production(Type1, UnityEngine.Random.Range(Minimum, Maximum));
        Result += new Production(Type2, UnityEngine.Random.Range(Minimum, Maximum));
        Result += new Production(Type3, UnityEngine.Random.Range(Minimum, Maximum));

        return Result;
    }

    protected void CreateUpgradeAt(int ChoiceIndex)
    {
        PrepareContainerForUpgrade(ChoiceIndex, out var Callback);
        Callback(ChoiceIndex);
    }

    protected void CreateRelicAt(int ChoiceIndex)
    {
        if (!Game.TryGetServices(out RelicService RelicService, out IconFactory IconFactory))
            return;

        if (!RelicService.UnlockableRelics.TryUnlockNewType(GetSeed() + ChoiceIndex, out var RelicType, true))
            return;

        PrepareContainerForRelic(ChoiceIndex, out Transform RelicContainer, out var Callback);
        IconFactory.CreateRelicIcon(RelicContainer, RelicService.Relics[RelicType], true);
        Callback(ChoiceIndex, RelicType);
    }

    protected void CreateAltarAt(int ChoiceIndex)
    {
        if (!Game.TryGetServices(out RelicService RelicService, out IconFactory IconFactory))
            return;

        PrepareContainerForAltar(ChoiceIndex, out var Callback);
        Callback(ChoiceIndex, ChoiceIndex == 0);
    }

    private void PrepareContainerForUpgrade(int ChoiceIndex, out Action<int> Callback)
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

    private void PrepareContainerForRelic(int ChoiceIndex, out Transform RelicContainer, out Action<int, RelicType> Callback)
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

    private void PrepareContainerForAltar(int ChoiceIndex, out Action<int, bool> Callback)
    {
        Transform TargetContainer = ChoiceContainers[ChoiceIndex];
        AddPrefabToContainer(TargetContainer, ChoicesPrefab[ChoiceTypes[ChoiceIndex]]);
        Button Button = TargetContainer.GetChild(0).GetChild(4).GetComponent<Button>();
        Button.onClick.RemoveAllListeners();
        Button.onClick.AddListener(() =>
        {
            OnSelectOption(ChoiceIndex);
        });
        Button.interactable = CanAffordChoice(ChoiceIndex);
        Callback = SetChoiceAltar;

        if (ChoiceIndex == 0)
        {
            PrepareContainerForAltarOffering();
        }
        else
        {
            PrepareContainerForAltarSacrifice();
        }
    }

    private void PrepareContainerForAltarOffering()
    {
        if (!Game.TryGetService(out IconFactory IconFactory))
            return;

        Transform TargetContainer = ChoiceContainers[0];
        TMPro.TextMeshProUGUI Name = TargetContainer.GetChild(0).GetChild(0).GetComponent<TMPro.TextMeshProUGUI>();
        TMPro.TextMeshProUGUI Flavour = TargetContainer.GetChild(0).GetChild(1).GetComponent<TMPro.TextMeshProUGUI>();
        Name.text = OfferingName;
        Flavour.text = OfferingFlavour;

        SVGImage Image = TargetContainer.GetChild(0).GetChild(2).GetComponent<SVGImage>();
        Image.sprite = IconFactory.GetIconForMisc(IconFactory.MiscellaneousType.Offering);

        Transform CostContainer = TargetContainer.GetChild(0).GetChild(3);
        GameObject Costs = IconFactory.GetVisualsForProduction(GetCostsForChoice(0), null, true).gameObject;
        Costs.transform.SetParent(CostContainer, false);
        RectTransform CostsRect = Costs.GetComponent<RectTransform>();
        CostsRect.anchorMin = new(0.5f, 0.5f);
        CostsRect.anchorMax = new(0.5f, 0.5f);
        CostsRect.anchoredPosition = new();

        TMPro.TextMeshProUGUI ButtonText = TargetContainer.GetChild(0).GetChild(4).GetChild(0).GetComponent<TMPro.TextMeshProUGUI>();
        ButtonText.text = OfferingButton;
    }

    private void PrepareContainerForAltarSacrifice()
    {
        if (!Game.TryGetService(out IconFactory IconFactory))
            return;

        Transform TargetContainer = ChoiceContainers[1];
        TMPro.TextMeshProUGUI Name = TargetContainer.GetChild(0).GetChild(0).GetComponent<TMPro.TextMeshProUGUI>();
        TMPro.TextMeshProUGUI Flavour = TargetContainer.GetChild(0).GetChild(1).GetComponent<TMPro.TextMeshProUGUI>();
        Name.text = SacrificeName;
        Flavour.text = SacrificeFlavour;

        SVGImage Image = TargetContainer.GetChild(0).GetChild(2).GetComponent<SVGImage>();
        Image.sprite = IconFactory.GetIconForMisc(IconFactory.MiscellaneousType.Sacrifice);

        Transform CostContainer = TargetContainer.GetChild(0).GetChild(3);
        GameObject Costs = IconFactory.GetVisualsForWorkerCost(null, true);
        Costs.transform.SetParent(CostContainer, false);

        TMPro.TextMeshProUGUI ButtonText = TargetContainer.GetChild(0).GetChild(4).GetChild(0).GetComponent<TMPro.TextMeshProUGUI>();
        ButtonText.text = SacrificeButton;
    }


    protected virtual void SetChoiceUpgrade(int Index)
    {
        Choices[Index] = new CollectableUpgradeChoice();
    }

    protected virtual void SetChoiceRelic(int Index, RelicType RelicType)
    {
        Choices[Index] = new CollectableRelicChoice(RelicType);
    }

    protected virtual void SetChoiceAltar(int Index, bool bIsOffering)
    {
        Choices[Index] = new CollectableAltarChoice(bIsOffering);
    }


    private bool CanSpawnUpgradeRuins()
    {
        if (!Game.TryGetServices(out BuildingService Buildings, out MapGenerator MapGenerator))
            return false;

        if (!MapGenerator.TryGetHexagonData(CurrentLocation, out var Hex))
            return false;

        // unlock current area buildings
        int TargetCategoryIndex = MapGenerator.UnlockableTypes.GetCategoryIndexOf(Hex.Type);
        if (!Buildings.UnlockableBuildings.HasCategoryAllUnlocked(TargetCategoryIndex))
            return true;

        // unlock the next area buildings
        TargetCategoryIndex++;
        if (TargetCategoryIndex >= Buildings.UnlockableBuildings.GetCategoryCount())
            return false;

        return !Buildings.UnlockableBuildings.HasCategoryAllUnlocked(TargetCategoryIndex);
    }


    private static string OfferingFlavour = "Buildings in 1 Range have 50% increased Production";
    private static string SacrificeFlavour = "Tiles in 1 Range cannot be affected by the Malaise";
    private static string OfferingName = "Offering";
    private static string SacrificeName = "Sacrifice";
    private static string OfferingButton = "Give Offering";
    private static string SacrificeButton = "Kill Innocent";
}
