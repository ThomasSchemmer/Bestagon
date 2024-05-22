using System;
using System.Security.AccessControl;
using TMPro;
using UnityEngine;

public class IconFactory : GameService
{
    public SerializedDictionary<Production.Type, Sprite> AvailableResources = new();
    public SerializedDictionary<HexagonConfig.HexagonType, Sprite> AvailableTiles = new();
    public SerializedDictionary<MiscellaneousType, Sprite> AvailableMiscellaneous = new();

    private GameObject ProductionGroupPrefab, NumberedIconPrefab, SimpleIconPrefab, ProduceEffectPrefab;
    private GameObject ProduceUnitEffectPrefab, GrantMiscPrefab, GrantResourceEventEffectPrefab;

    public enum MiscellaneousType
    {
        TrendUp,
        TrendDown,
        Worker,
        Scout,
        Usages,
        RemoveMalaise
    }

    public void Refresh()
    {
        LoadResources();
        LoadTiles();
        LoadMiscellaneous();
        LoadPrefabs();
    }


    private void LoadPrefabs()
    {
        ProductionGroupPrefab = Resources.Load("UI/ProductionGroup") as GameObject;
        NumberedIconPrefab = Resources.Load("UI/NumberedIcon") as GameObject;
        SimpleIconPrefab = Resources.Load("UI/SimpleIcon") as GameObject;
        ProduceEffectPrefab = Resources.Load("UI/Cards/ProduceEffect") as GameObject;
        ProduceUnitEffectPrefab = Resources.Load("UI/Cards/ProduceUnitEffect") as GameObject;
        GrantMiscPrefab = Resources.Load("UI/Cards/GrantUnitEventEffect") as GameObject;
        GrantResourceEventEffectPrefab = Resources.Load("UI/Cards/GrantResourceEventEffect") as GameObject;
    }

    private void LoadResources()
    {
        AvailableResources.Clear();
        var ResourceTypes = Enum.GetValues(typeof(Production.Type));
        foreach (var ResourceType in ResourceTypes)
        {
            GameObject MeshObject = Resources.Load("Icons/" + ResourceType) as GameObject;
            if (!MeshObject || !MeshObject.GetComponent<SpriteRenderer>())
                continue;

            Sprite Sprite = MeshObject.GetComponent<SpriteRenderer>().sprite;
            if (!Sprite)
                continue;

            AvailableResources.Add((Production.Type)ResourceType, Sprite);
        }
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
            GameObject MeshObject = Resources.Load("Icons/" + MiscType) as GameObject;
            if (!MeshObject || !MeshObject.GetComponent<SpriteRenderer>())
                continue;

            Sprite Sprite = MeshObject.GetComponent<SpriteRenderer>().sprite;
            if (!Sprite)
                continue;

            AvailableMiscellaneous.Add((MiscellaneousType)MiscType, Sprite);
        }
    }

    public Sprite GetIconForProduction(Production.Type Type)
    {
        if (!AvailableResources.ContainsKey(Type))
            return null;

        return AvailableResources[Type];
    }

    public Sprite GetIconForTile(HexagonConfig.HexagonType Type)
    {
        if (!AvailableTiles.ContainsKey(Type))
            return null;

        return AvailableTiles[Type];
    }

    public GameObject GetVisualsForProduction(Production Production, ISelectable Parent)
    {
        var Tuples = Production.GetTuples();
        GameObject ProductionGroup = Instantiate(ProductionGroupPrefab);
        RectTransform GroupTransform = ProductionGroup.GetComponent<RectTransform>();
        int Width = 62;
        int XOffset = Width / 2;
        GroupTransform.sizeDelta = new(Width * Tuples.Count, 30);
        for (int i = 0; i < Tuples.Count; i++)
        {
            Tuple<Production.Type, int> Tuple = Tuples[i];
            GameObject ProductionUnit = Instantiate(NumberedIconPrefab);
            RectTransform UnitTransform = ProductionUnit.GetComponent<RectTransform>();
            UnitTransform.SetParent(GroupTransform, false);
            UnitTransform.localPosition = new(XOffset + i * Width, 0, 0);
            NumberedIconScreen UnitScreen = ProductionUnit.GetComponent<NumberedIconScreen>();

            UnitScreen.Initialize(GetIconForProduction(Tuple.Key), true, Tuple.Key.ToString(), Parent);
            UnitScreen.UpdateVisuals(Tuple.Value);
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
        GameObject ProductionGO = GetVisualsForProduction(Effect.Production, Parent);
        ProductionGO.transform.SetParent(ProductionContainer, false);
        GameObject HexTypesGO = GetVisualsForHexTypes(Effect.TileType, Parent);
        HexTypesGO.transform.SetParent(TypeContainer, false);
        return ProductionEffect;
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
        GameObject ConsumptionGO = GetVisualsForProduction(Effect.Consumption, Parent);
        ConsumptionGO.transform.SetParent(ConsumesContainer, false);

        bool bConsumes = !Effect.Consumption.Equals(Production.Empty);
        ConsumesText.gameObject.SetActive(bConsumes);
        ConsumesContainer.gameObject.SetActive(bConsumes);

        return ProduceUnitEffect;
    }

    public GameObject GetVisualsForGrantUnitEffect(GrantUnitEventData EventData, ISelectable Parent)
    {
        if (!TryGetMiscFromUnit(EventData.GrantedType, out MiscellaneousType UnitType))
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

        GameObject UnitTypeGO = GetVisualsForProduction(EventData.GrantedResource, Parent);
        UnitTypeGO.transform.SetParent(UnitTypeContainer, false);
        UnitTypeGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(31, 0);

        return ProduceUnitEffect;
    }

    public GameObject GetVisualsForMiscalleneous(MiscellaneousType Type, ISelectable Parent, int Amount)
    {
        GameObject MiscUnit = Instantiate(NumberedIconPrefab);
        RectTransform RectTransform = MiscUnit.GetComponent<RectTransform>();
        RectTransform.localPosition = Vector3.zero;
        NumberedIconScreen IconScreen = MiscUnit.GetComponent<NumberedIconScreen>();

        IconScreen.Initialize(GetIconForMisc(Type), false, Type.ToString(), Parent);
        IconScreen.UpdateVisuals(Amount);
        return MiscUnit;
    }

    public bool TryGetMiscFromUnit(UnitData.UnitType Type, out MiscellaneousType MiscType)
    {
        switch (Type)
        {
            case UnitData.UnitType.Worker: MiscType = MiscellaneousType.Worker; return true;
            case UnitData.UnitType.Scout: MiscType = MiscellaneousType.Scout; return true;
            default: MiscType = MiscellaneousType.Worker; return false;
        }
    }

    public GameObject GetVisualsForConvertTileEvent(ConvertTileEventData EventData, ISelectable Parent)
    {
        GameObject ProduceUnitEffect = Instantiate(GrantMiscPrefab);
        TextMeshProUGUI ProducesText = ProduceUnitEffect.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
        ProducesText.text = EventData.GetDescription();
        Transform UnitTypeContainer = ProduceUnitEffect.transform.GetChild(1);

        GameObject UnitTypeGO = GetVisualsForHexTypes(EventData.TargetType, Parent);
        UnitTypeGO.transform.SetParent(UnitTypeContainer, false);
        UnitTypeGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(31, 0);

        return ProduceUnitEffect;
    }

    public GameObject GetVisualsForHexTypes(HexagonConfig.HexagonType Types, ISelectable Parent)
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

            IconScreen.Initialize(GetIconForTile(Type), false, Type.ToString(), Parent);
            Index++;
        }
        return ProductionGroup;
    }

    public Sprite GetIconForMisc(MiscellaneousType Type)
    {
        if (!AvailableMiscellaneous.ContainsKey(Type))
            return null;

        return AvailableMiscellaneous[Type];
    }

    protected override void StartServiceInternal()
    {
        Refresh();
        _OnInit?.Invoke();
    }

    protected override void StopServiceInternal() { }
}
