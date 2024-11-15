using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/** Represents an Indicator that shows a production */
[RequireComponent(typeof(BuildingPreview))]
public class ProductionIndicator : IndicatorComponent
{
    private BuildingPreview Preview;
    private Location LastLocation = Location.Invalid;

    protected override void Initialize()
    {
        Preview = GetComponent<BuildingPreview>();
    }

    protected override int GetTargetLayer()
    {
        return LayerMask.NameToLayer("UI") ;
    }

    protected override int GetIndicatorAmount()
    {
        return GetNeighbourLocations().Count;
    }

    public override IndicatorService.IndicatorType GetIndicatorType()
    {
        return IndicatorService.IndicatorType.NumberedIcon;
    }

    private HashSet<Location> GetNeighbourLocations()
    {
        if (!Game.TryGetService(out Selectors Selectors))
            return new();

        HexagonVisualization HoveredHex = Selectors.GetHoveredHexagon();
        if (HoveredHex == null)
            return new();

        LastLocation = HoveredHex.Location;
        BuildingEntity Building = Preview.GetPreviewable() as BuildingEntity;
        if (Building == null)
            return new();

        if (!LocationSet.TryGetAround(HoveredHex.Location, Building.Area, out var Area))
            return new();

        int Range = Building.Effect.Range;
        bool bIsAdjacentCard = Range > 0;
        return MapGenerator.GetNeighbourTileLocationsInRange(Area, !bIsAdjacentCard, Range);
    }

    protected override GameObject InstantiateIndicator(int i, RectTransform Parent)
    {
        // create an empty parent, as the child needs to be recreated every time,
        // as the ProductionGroup can have different width etc
        GameObject Base = new();
        Base.transform.SetParent(Parent);
        Base.AddComponent<RectTransform>();

        return Base;
    }

    private void AddVisualsForProduction(GameObject Base, Production Production)
    {
        if (!Game.TryGetService(out IconFactory IconFactory))
            return;

        if (Base.transform.childCount > 0)
        {
            DestroyImmediate(Base.transform.GetChild(0).gameObject);
        }

        GameObject ProductionIndicator = IconFactory.GetVisualsForNumberedIndicator(Base.transform);
        GameObject ProductionGroupGO = IconFactory.GetVisualsForProduction(Production, null, false, true);
        ProductionGroupGO.transform.SetParent(ProductionIndicator.transform, false);
        ProductionIndicator.transform.SetParent(Base.transform, false);
        ProductionIndicator.GetComponent<RectTransform>().localPosition = new(0, 0, 0);

        bool bIsEmpty = Production.IsEmpty();
        Base.SetActive(!bIsEmpty);
        ProductionIndicator.SetActive(!bIsEmpty);
        ProductionGroupGO.SetActive(!bIsEmpty);
    }

    private Production GetProductionForNeighbour(int i)
    {
        BuildingEntity Building = Preview.GetPreviewable() as BuildingEntity;
        if (Building == null)
            return Production.Empty;

        if (!Building.TryGetAdjacencyBonus(out var Boni))
            return Production.Empty;

        HashSet<Location> Neighbours = GetNeighbourLocations();
        if (Neighbours.Count == 0)
            return Production.Empty;

        Location Location = Neighbours.ToList()[i];
        if (!Game.TryGetService(out MapGenerator MapGenerator))
            return Production.Empty;

        if (!MapGenerator.TryGetHexagonData(Location, out var Hex))
            return Production.Empty;

        if (!Boni.TryGetValue(Hex.Type, out Production Bonus))
            return Production.Empty;

        float Multiplier = AttributeSet.Get()[AttributeType.ProductionRate].GetAt(Location);
        return Multiplier * Bonus;
    }

    protected override void ApplyIndicatorScreenPosition(int i, RectTransform IndicatorTransform)
    {
        if (Indicators[i] == null)
            return;

        HashSet<Location> Neighbours = GetNeighbourLocations();
        if (Neighbours.Count == 0) 
            return;

        Location Location = Neighbours.ToList()[i];
        Vector3 WorldPos = HexagonConfig.TileSpaceToWorldSpace(Location.GlobalTileLocation);
        WorldPos += new Vector3(0, HexagonConfig.TileSize.y, 0);
        IndicatorTransform.position = Service.WorldPosToScreenPos(WorldPos);
    }

    protected override void UpdateIndicatorVisuals(int i)
    {
        if (Indicators[i] == null)
            return;

        Production Production = GetProductionForNeighbour(i);
        AddVisualsForProduction(Indicators[i].gameObject, Production);
    }

    public override bool NeedsVisualUpdate()
    {
        if (!gameObject.activeSelf)
            return false;

        if (LastLocation.Equals(Location.Invalid))
            return false;

        if (!Game.TryGetService(out Selectors Selectors))
            return false;

        HexagonVisualization HoveredHex = Selectors.GetHoveredHexagon();
        if (HoveredHex == null)
            return false;

        return !HoveredHex.Equals(LastLocation);
    }
}
