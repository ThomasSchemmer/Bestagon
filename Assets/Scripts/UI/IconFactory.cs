using System;
using System.Security.AccessControl;
using UnityEngine;

public class IconFactory : GameService
{
    public SerializedDictionary<Production.Type, Sprite> AvailableResources = new();
    public SerializedDictionary<MiscellaneousType, Sprite> AvailableMiscellaneous = new();

    private GameObject ProductionGroupPrefab, ProductionUnitPrefab;

    public enum MiscellaneousType
    {
        TrendStable,
        TrendUp,
        TrendDown,
        Worker,
        Usages
    }

    public void Refresh()
    {
        LoadResources();
        LoadMiscellaneous();
        LoadPrefabs();
    }

    private void LoadPrefabs()
    {
        ProductionGroupPrefab = Resources.Load("UI/ProductionGroup") as GameObject;
        ProductionUnitPrefab = Resources.Load("UI/ProductionUnit") as GameObject;
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

    public Sprite GetIconForType(Production.Type Type)
    {
        if (!AvailableResources.ContainsKey(Type))
            return null;

        return AvailableResources[Type];
    }

    public GameObject GetVisualsForProduction(Production Production)
    {
        var Tuples = Production.GetTuples();
        GameObject ProductionGroup = Instantiate(ProductionGroupPrefab);
        RectTransform GroupTransform = ProductionGroup.GetComponent<RectTransform>();
        int Width = 46;
        int XOffset = Width / 2;
        GroupTransform.sizeDelta = new(Width * Tuples.Count, 30);
        for (int i = 0; i < Tuples.Count; i++)
        {
            Tuple<Production.Type, int> Tuple = Tuples[i];
            GameObject ProductionUnit = Instantiate(ProductionUnitPrefab);
            RectTransform UnitTransform = ProductionUnit.GetComponent<RectTransform>();
            UnitTransform.SetParent(GroupTransform, false);
            UnitTransform.localPosition = new(XOffset + i * Width, 0, 0);
            UnitScreen UnitScreen = ProductionUnit.GetComponent<UnitScreen>();
            UnitScreen.Initialize(GetIconForType(Tuple.Key), false);
            UnitScreen.UpdateVisuals(Tuple.Value);
        }
        return ProductionGroup;
    }

    public GameObject GetVisualsForMiscalleneous(MiscellaneousType Type, int Amount = 0)
    {
        GameObject MiscUnit = Instantiate(ProductionUnitPrefab);
        RectTransform RectTransform = MiscUnit.GetComponent<RectTransform>();
        RectTransform.localPosition = Vector3.zero;
        UnitScreen MiscScreen = MiscUnit.GetComponent<UnitScreen>();
        MiscScreen.Initialize(GetIconForMisc(Type), false);
        MiscScreen.UpdateVisuals(Amount);
        return MiscUnit;
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
