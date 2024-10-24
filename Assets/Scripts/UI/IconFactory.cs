using System;
using System.Security.AccessControl;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class IconFactory : GameService
{
    public SerializedDictionary<Production.Type, Sprite> AvailableResources = new();
    public SerializedDictionary<Production.GoodsType, Sprite> AvailableResourceTypes = new();
    public SerializedDictionary<HexagonConfig.HexagonType, Sprite> AvailableTiles = new();
    public SerializedDictionary<BuildingConfig.Type, Sprite> AvailableBuildingTypes = new();
    public SerializedDictionary<MiscellaneousType, Sprite> AvailableMiscellaneous = new();

    private GameObject ProductionGroupPrefab, NumberedIconPrefab, NumberedIconHighlightedPrefab, SimpleIconPrefab, ProduceEffectPrefab;
    private GameObject ProduceUnitEffectPrefab, GrantMiscPrefab, GrantResourceEventEffectPrefab;
    private GameObject ProduceConsumeEffectPrefab;
    private GameObject UpgradeButtonPrefab;
    private GameObject RelicIconPrefab, RelicGroupPrefab, RelicIconPreviewPrefab;
    private GameObject CardGroupPrefab;

    private Sprite PlaceholderSprite;

    public enum MiscellaneousType
    {
        Worker,
        Scout,
        Usages,
        RemoveMalaise,
        WorkerIndicator,
        NoWorkerIndicator,
        Buildings,
        Camera,
        Tile,
        Abandon,
        Upgrades,
        Pin,
        PinActive,
        UnknownRelic,
        Relic
    }

    public void Refresh()
    {
        LoadResources();
        LoadResourceTypes();
        LoadTiles();
        LoadBuildingTypes();
        LoadMiscellaneous();
        LoadPlaceholder();
        LoadPrefabs();
    }


    private void LoadPrefabs()
    {
        ProductionGroupPrefab = Resources.Load("UI/ProductionGroup") as GameObject;
        NumberedIconPrefab = Resources.Load("UI/NumberedIcon") as GameObject;
        NumberedIconHighlightedPrefab = Resources.Load("UI/NumberedIconHighlighted") as GameObject;
        SimpleIconPrefab = Resources.Load("UI/SimpleIcon") as GameObject;
        ProduceEffectPrefab = Resources.Load("UI/Cards/ProduceEffect") as GameObject;
        ProduceUnitEffectPrefab = Resources.Load("UI/Cards/ProduceUnitEffect") as GameObject;
        GrantMiscPrefab = Resources.Load("UI/Cards/GrantUnitEventEffect") as GameObject;
        GrantResourceEventEffectPrefab = Resources.Load("UI/Cards/GrantResourceEventEffect") as GameObject;
        ProduceConsumeEffectPrefab = Resources.Load("UI/Cards/ProduceConsumeEffect") as GameObject;
        UpgradeButtonPrefab = Resources.Load("UI/UpgradeButton") as GameObject;


        RelicIconPrefab = Resources.Load("UI/Relics/RelicIcon") as GameObject;
        RelicIconPreviewPrefab = Resources.Load("UI/Relics/RelicIconPreview") as GameObject;
        RelicGroupPrefab = Resources.Load("UI/Relics/RelicGroup") as GameObject;

        CardGroupPrefab = Resources.Load("UI/CardGroup") as GameObject;
    }
    private void LoadResources()
    {
        AvailableResources.Clear();
        var ResourceTypes = Enum.GetValues(typeof(Production.Type));
        foreach (var ResourceType in ResourceTypes)
        {
            GameObject MeshObject = Resources.Load("Icons/Production/" + ResourceType) as GameObject;
            if (!MeshObject || !MeshObject.GetComponent<SpriteRenderer>())
                continue;

            Sprite Sprite = MeshObject.GetComponent<SpriteRenderer>().sprite;
            if (!Sprite)
                continue;

            AvailableResources.Add((Production.Type)ResourceType, Sprite);
        }
    }

    private void LoadResourceTypes()
    {
        AvailableResourceTypes.Clear();
        var ResourceTypes = Enum.GetValues(typeof(Production.GoodsType));
        foreach (var ResourceType in ResourceTypes)
        {
            GameObject MeshObject = Resources.Load("Icons/Production/" + ResourceType) as GameObject;
            if (!MeshObject || !MeshObject.GetComponent<SpriteRenderer>())
                continue;

            Sprite Sprite = MeshObject.GetComponent<SpriteRenderer>().sprite;
            if (!Sprite)
                continue;

            AvailableResourceTypes.Add((Production.GoodsType)ResourceType, Sprite);
        }
    }

    private void LoadBuildingTypes()
    {
        AvailableBuildingTypes.Clear();
        var BuildingTypes = Enum.GetValues(typeof(BuildingConfig.Type));
        foreach (var BuildingType in BuildingTypes)
        {
            GameObject MeshObject = Resources.Load("Icons/Buildings/" + BuildingType) as GameObject;
            if (!MeshObject || !MeshObject.GetComponent<SpriteRenderer>())
                continue;

            Sprite Sprite = MeshObject.GetComponent<SpriteRenderer>().sprite;
            if (!Sprite)
                continue;

            AvailableBuildingTypes.Add((BuildingConfig.Type)BuildingType, Sprite);
        }
    }

    private void LoadPlaceholder()
    {
        GameObject MeshObject = Resources.Load("Icons/UI/Placeholder") as GameObject;
        if (!MeshObject || !MeshObject.GetComponent<SpriteRenderer>())
            return;

        Sprite Sprite = MeshObject.GetComponent<SpriteRenderer>().sprite;
        if (!Sprite)
            return;

        PlaceholderSprite = Sprite;
    }


    private void LoadTiles()
    {
        AvailableTiles.Clear();
        var TileTypes = Enum.GetValues(typeof(HexagonConfig.HexagonType));
        foreach (var TileType in TileTypes)
        {
            GameObject MeshObject = Resources.Load("Icons/Tiles/" + TileType) as GameObject;
            if (!MeshObject || !MeshObject.GetComponent<SpriteRenderer>())
                continue;

            Sprite Sprite = MeshObject.GetComponent<SpriteRenderer>().sprite;
            if (!Sprite)
                continue;

            AvailableTiles.Add((HexagonConfig.HexagonType)TileType, Sprite);
        }
    }

    private void LoadMiscellaneous()
    {
        AvailableMiscellaneous.Clear();
        var MiscTypes = Enum.GetValues(typeof(MiscellaneousType));
        foreach (var MiscType in MiscTypes)
        {
            GameObject MeshObject = Resources.Load("Icons/Misc/" + MiscType) as GameObject;
            if (!MeshObject || !MeshObject.GetComponent<SpriteRenderer>())
                continue;

            Sprite Sprite = MeshObject.GetComponent<SpriteRenderer>().sprite;
            if (!Sprite)
                continue;

            AvailableMiscellaneous.Add((MiscellaneousType)MiscType, Sprite);
        }
    }


    public Sprite GetIconForProductionType(Production.GoodsType Type)
    {
        if (!AvailableResourceTypes.ContainsKey(Type))
            return PlaceholderSprite;

        return AvailableResourceTypes[Type];

    }
    public Sprite GetIconForProduction(Production.Type Type)
    {
        if (!AvailableResources.ContainsKey(Type))
            return PlaceholderSprite;

        return AvailableResources[Type];
    }

    public Sprite GetIconForTile(HexagonConfig.HexagonType Type)
    {
        if (!AvailableTiles.ContainsKey(Type))
            return PlaceholderSprite;

        return AvailableTiles[Type];
    }

    public Sprite GetIconForMisc(MiscellaneousType Type)
    {
        if (!AvailableMiscellaneous.ContainsKey(Type))
            return PlaceholderSprite;

        return AvailableMiscellaneous[Type];
    }

    public Sprite GetIconForBuildingType(BuildingConfig.Type Type)
    {
        if (!AvailableBuildingTypes.ContainsKey(Type))
            return PlaceholderSprite;

        return AvailableBuildingTypes[Type];
    }

    public GameObject GetVisualsForProduction(Production Production, ISelectable Parent, bool bSubscribe, bool bIgnoreClicks = false)
    {
        var Tuples = Production.GetTuples();
        GameObject ProductionGroup = Instantiate(ProductionGroupPrefab);
        RectTransform GroupTransform = ProductionGroup.GetComponent<RectTransform>();
        int Width = 62;
        GroupTransform.sizeDelta = new(Width * Tuples.Count, 30);
        GroupTransform.anchoredPosition = new Vector2(GroupTransform.sizeDelta.x / 2f, 0);
        float XOffset = (Tuples.Count % 2) == 0 ? -Width / 2f : 0;

        for (int i = 0; i < Tuples.Count; i++)
        {
            Tuple<Production.Type, int> Tuple = Tuples[i];
            GameObject ProductionUnit = Instantiate(NumberedIconPrefab);
            RectTransform UnitTransform = ProductionUnit.GetComponent<RectTransform>();
            UnitTransform.SetParent(GroupTransform, false);
            UnitTransform.localPosition = new(i * Width + XOffset, 0, 0);
            NumberedIconScreen UnitScreen = ProductionUnit.GetComponent<NumberedIconScreen>();

            UnitScreen.Initialize(GetIconForProduction(Tuple.Key), Tuple.Key.ToString(), Parent);
            UnitScreen.SetIgnored(bIgnoreClicks);
            UnitScreen.UpdateVisuals(Tuple.Value);
            if (bSubscribe)
            {
                UnitScreen.SetSubscription(Tuple.Key, Tuple.Value);
            }
        }
        return ProductionGroup;
    }

    public GameObject GetVisualsForProduceEffect(OnTurnBuildingEffect Effect, ISelectable Parent)
    {
        GameObject ProductionEffect = Instantiate(ProduceEffectPrefab);
        Transform ProductionContainer = ProductionEffect.transform.GetChild(1);
        TextMeshProUGUI AdjacentText = ProductionEffect.transform.GetChild(2).GetComponent<TextMeshProUGUI>();
        AdjacentText.text = Effect.GetDescription();
        Transform TypeContainer = ProductionEffect.transform.GetChild(3);
        GameObject ProductionGO = GetVisualsForProduction(Effect.Production, Parent, false);
        ProductionGO.transform.SetParent(ProductionContainer, false);
        GameObject HexTypesGO = GetVisualsForHexTypes(Effect.TileType, Parent);
        HexTypesGO.transform.SetParent(TypeContainer, false);
        return ProductionEffect;
    }

    public GameObject GetVisualsForMerchantEffect(OnTurnBuildingEffect Effect, ISelectable Parent)
    {
        GameObject MerchantEffect = Instantiate(ProduceEffectPrefab);
        TextMeshProUGUI MerchantText = MerchantEffect.transform.GetChild(2).GetComponent<TextMeshProUGUI>();
        MerchantText.text = Effect.GetDescription();
        Transform TypeContainer = MerchantEffect.transform.GetChild(3);
        DestroyImmediate(TypeContainer.gameObject);
        return MerchantEffect;
    }

    public GameObject GetVisualsForProduceUnitEffect(OnTurnBuildingEffect Effect, ISelectable Parent)
    {
        GameObject ProduceUnitEffect = Instantiate(ProduceUnitEffectPrefab);
        TextMeshProUGUI ProducesText = ProduceUnitEffect.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
        ProducesText.text = Effect.GetDescription();
        Transform UnitTypeContainer = ProduceUnitEffect.transform.GetChild(1);
        if (!TryGetMiscFromUnit(Effect.UnitType, out MiscellaneousType UnitType))
            return null;

        GameObject UnitTypeGO = GetVisualsForMiscalleneous(UnitType, Parent, 1);
        UnitTypeGO.transform.SetParent(UnitTypeContainer, false);
        UnitTypeGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(31, 0);

        TextMeshProUGUI ConsumesText = ProduceUnitEffect.transform.GetChild(2).GetComponent<TextMeshProUGUI>();
        ConsumesText.text = Effect.GetDescriptionProduceUnitConsumption();
        Transform ConsumesContainer = ProduceUnitEffect.transform.GetChild(3);
        GameObject ConsumptionGO = GetVisualsForProduction(Effect.Consumption, Parent, true);
        ConsumptionGO.transform.SetParent(ConsumesContainer, false);

        bool bConsumes = !Effect.Consumption.Equals(Production.Empty);
        ConsumesText.gameObject.SetActive(bConsumes);
        ConsumesContainer.gameObject.SetActive(bConsumes);

        return ProduceUnitEffect;
    }

    public GameObject GetVisualsForProduceConsumeEffect(OnTurnBuildingEffect Effect, ISelectable Parent)
    {
        GameObject ProductionEffect = Instantiate(ProduceConsumeEffectPrefab);
        Transform ProductionContainer = ProductionEffect.transform.GetChild(1);
        TextMeshProUGUI ConsumptionText = ProductionEffect.transform.GetChild(2).GetComponent<TextMeshProUGUI>();
        ConsumptionText.text = Effect.GetDescription();
        Transform ConsumptionContainer = ProductionEffect.transform.GetChild(3);
        GameObject ProductionGO = GetVisualsForProduction(Effect.Production, Parent, false);
        ProductionGO.transform.SetParent(ProductionContainer, false);
        GameObject ConsumptionGO = GetVisualsForProduction(Effect.Consumption, Parent, true);
        ConsumptionGO.transform.SetParent(ConsumptionContainer, false);
        return ProductionEffect;
    }

    public GameObject GetVisualsForGrantUnitEffect(GrantUnitEventData EventData, ISelectable Parent)
    {
        if (!TryGetMiscFromUnit(EventData.GrantedUnitType, out MiscellaneousType UnitType))
            return null;

        return GetVisualsForMiscalleneousEffect(EventData, UnitType, Parent);
    }

    private GameObject GetVisualsForMiscalleneousEffect(EventData EventData, MiscellaneousType MiscType, ISelectable Parent)
    {

        GameObject ProduceUnitEffect = Instantiate(GrantMiscPrefab);
        TextMeshProUGUI ProducesText = ProduceUnitEffect.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
        ProducesText.text = EventData.GetDescription();
        Transform UnitTypeContainer = ProduceUnitEffect.transform.GetChild(1);

        GameObject UnitTypeGO = GetVisualsForMiscalleneous(MiscType, Parent, 1);
        UnitTypeGO.transform.SetParent(UnitTypeContainer, false);
        UnitTypeGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(31, 0);

        return ProduceUnitEffect;
    }

    public RelicGroupScreen CreateRelicGroup(Transform Container, SerializedDictionary<RelicType, Unlockables.State> Category, int Tier)
    {
        GameObject GO = Instantiate(RelicGroupPrefab);
        RelicGroupScreen RelicGroup = GO.GetComponent<RelicGroupScreen>();
        RelicGroup.InitializeRelics(Category, Tier);
        RelicGroup.transform.SetParent(Container, false);
        return RelicGroup;
    }

    public RelicIconScreen CreateRelicIcon(Transform Container, RelicEffect Relic, bool bIsPreview)
    {
        GameObject GO = Instantiate(bIsPreview ? RelicIconPreviewPrefab : RelicIconPrefab);
        RelicIconScreen RelicIcon = GO.GetComponent<RelicIconScreen>();
        RelicIcon.Initialize(Relic, bIsPreview);
        RelicIcon.transform.SetParent(Container, false);
        return RelicIcon;
    }

    public CardGroupScreen CreateCardGroupScreen(Transform Container)
    {
        GameObject GO = Instantiate(CardGroupPrefab);
        CardGroupScreen CardGroup = GO.GetComponent<CardGroupScreen>();
        CardGroup.transform.SetParent(Container, false);
        return CardGroup;
    }

    public GameObject GetVisualsForRemoveMalaiseEffect(RemoveMalaiseEventData EventData, ISelectable Parent)
    {
        return GetVisualsForMiscalleneousEffect(EventData, MiscellaneousType.RemoveMalaise, Parent);
    }

    public GameObject GetVisualsForGrantResourceEffect(GrantResourceEventData EventData, ISelectable Parent)
    {
        GameObject ProduceUnitEffect = Instantiate(GrantResourceEventEffectPrefab);
        TextMeshProUGUI ProducesText = ProduceUnitEffect.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
        ProducesText.text = EventData.GetDescription();
        Transform UnitTypeContainer = ProduceUnitEffect.transform.GetChild(1);

        GameObject UnitTypeGO = GetVisualsForProduction(EventData.GrantedResource, Parent, false);
        UnitTypeGO.transform.SetParent(UnitTypeContainer, false);
        UnitTypeGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(31, 0);

        return ProduceUnitEffect;
    }

    public GameObject GetVisualsForMiscalleneous(MiscellaneousType Type, ISelectable Parent, int Amount, bool bIgnore = false, bool bHighlight = false)
    {
        GameObject MiscUnit = Instantiate(bHighlight ? NumberedIconHighlightedPrefab : NumberedIconPrefab);
        NumberedIconScreen IconScreen = MiscUnit.GetComponent<NumberedIconScreen>();

        IconScreen.Initialize(GetIconForMisc(Type), Type.ToString(), Parent);
        IconScreen.SetIgnored(bIgnore);
        IconScreen.UpdateVisuals(Amount);
        return MiscUnit;
    }

    public bool TryGetMiscFromUnit(UnitEntity.UType Type, out MiscellaneousType MiscType)
    {
        switch (Type)
        {
            case UnitEntity.UType.Worker: MiscType = MiscellaneousType.Worker; return true;
            case UnitEntity.UType.Scout: MiscType = MiscellaneousType.Scout; return true;
            default: MiscType = MiscellaneousType.Worker; return false;
        }
    }

    public GameObject GetVisualsForConvertTileEvent(ConvertTileEventData EventData, ISelectable Parent)
    {
        GameObject ProduceUnitEffect = Instantiate(GrantMiscPrefab);
        TextMeshProUGUI ProducesText = ProduceUnitEffect.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
        ProducesText.text = EventData.GetDescription();
        Transform UnitTypeContainer = ProduceUnitEffect.transform.GetChild(1);

        GameObject UnitTypeGO = GetVisualsForHexTypes(EventData.TargetHexType, Parent);
        UnitTypeGO.transform.SetParent(UnitTypeContainer, false);
        UnitTypeGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(31, 0);

        return ProduceUnitEffect;
    }

    public GameObject GetVisualsForHexTypes(HexagonConfig.HexagonType Types, ISelectable Parent, bool bIgnore = false)
    {
        GameObject ProductionGroup = Instantiate(ProductionGroupPrefab);
        RectTransform GroupTransform = ProductionGroup.GetComponent<RectTransform>();
        int Width = 30;
        int XOffset = Width / 2;
        int Count = HexagonConfig.GetSetBitsAmount((int)Types);
        GroupTransform.sizeDelta = new(Width * Count, 30);
        int Index = 0;
        for (int i = 0; i < 32; i++)
        {
            if ((((int)Types >> i) & 0x1) == 0)
                continue;

            HexagonConfig.HexagonType Type = (HexagonConfig.HexagonType)(((int)Types) & (1 << i));
            GameObject SimpleIcon = Instantiate(SimpleIconPrefab);
            RectTransform IconTransform = SimpleIcon.GetComponent<RectTransform>();
            IconTransform.SetParent(GroupTransform, false);
            IconTransform.localPosition = new(XOffset + Index * Width, 0, 0);
            SimpleIconScreen IconScreen = SimpleIcon.GetComponent<SimpleIconScreen>();
            IconScreen.SetIgnored(bIgnore);

            IconScreen.Initialize(GetIconForTile(Type), Type.ToString(), Parent);
            Index++;
        }
        return ProductionGroup;
    }

    public GameObject ConvertVisualsToButton(Transform ParentTransform, RectTransform TransformToBeConverted)
    {
        Button Button = Instantiate(UpgradeButtonPrefab).GetComponent<Button>();
        RectTransform ButtonTransform = Button.GetComponent<RectTransform>();
        ButtonTransform.anchorMin = TransformToBeConverted.anchorMin;
        ButtonTransform.anchorMax = TransformToBeConverted.anchorMax;
        ButtonTransform.sizeDelta = TransformToBeConverted.sizeDelta;
        ButtonTransform.SetParent(ParentTransform, false);
        ButtonTransform.anchoredPosition = TransformToBeConverted.anchoredPosition;
        TransformToBeConverted.SetParent(ButtonTransform, true);

        return ButtonTransform.gameObject;
    }

    protected override void StartServiceInternal()
    {
        Refresh();
        _OnInit?.Invoke(this);
    }

    protected override void StopServiceInternal() { }
}
