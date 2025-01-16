using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.XR;

/** Represents an Indicator that shows a production */
[RequireComponent(typeof(BuildingPreview))]
public class ProductionIndicator : IndicatorComponent
{
    private BuildingPreview Preview;
    private Location LastLocation = Location.Invalid;

    private GameObject[] ProductionIndicators;
    private GameObject[] ProductionGroupGOs;
    private ProductionGroup[] ProductionGroups;

    private Production[] NeighbourProductions;

    protected override void Initialize()
    {
        Preview = GetComponent<BuildingPreview>();
        int Count = GetIndicatorAmount();
        ProductionIndicators = new GameObject[Count];
        ProductionGroupGOs = new GameObject[Count];
        ProductionGroups = new ProductionGroup[Count];
        NeighbourProductions = new Production[Count];
        for (int i = 0; i < NeighbourProductions.Length; i++)
        {
            NeighbourProductions[i] = Production.Empty;
        }
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

    private bool TryGetBuildingInfo(out BuildingEntity Building, out LocationSet Location, out int Range)
    {
        Range = -1;
        Location = null;
        Building = null;
        if (!Game.TryGetService(out Selectors Selectors))
            return false;

        HexagonVisualization HoveredHex = Selectors.GetHoveredHexagon();
        if (HoveredHex == null)
            return false;

        LastLocation = HoveredHex.Location;
        Building = Preview.GetPreviewable() as BuildingEntity;
        if (Building == null)
            return false;

        if (!LocationSet.TryGetAround(HoveredHex.Location, Building.Area, out var Area))
            return false;

        Location = Area;
        Range = Building.Effect.Range;
        return true;
    }

    private HashSet<Location> GetNeighbourLocations()
    {
        if (!TryGetBuildingInfo(out var _, out var Area, out var Range))
            return new();

        bool bIsAdjacentCard = Range > 0;
        return MapGenerator.GetNeighbourTileLocationsInRange(Area, !bIsAdjacentCard, Range);
    }

    protected override GameObject InstantiateIndicator(int i, RectTransform Parent)
    {
        Profiler.BeginSample("ProductionIndicator.InstantiateIndicator");
        if (!Game.TryGetService(out IconFactory IconFactory))
            return null;

        // create an empty parent, as the child needs to be recreated every time,
        // as the ProductionGroup can have different width etc
        GameObject Base = new();
        Base.transform.SetParent(Parent);
        Base.AddComponent<RectTransform>();

        GameObject ProductionIndicator = IconFactory.GetVisualsForNumberedIndicator(Base.transform);
        ProductionGroup ProductionGroup = IconFactory.GetVisualsForProduction(Production.Empty, null, false, true);
        GameObject ProductionGO = ProductionGroup.gameObject;

        ProductionGO.transform.SetParent(ProductionIndicator.transform, false);
        ProductionIndicator.transform.SetParent(Base.transform, false);
        ProductionIndicator.GetComponent<RectTransform>().localPosition = new(0, 0, 0);
        ProductionIndicators[i] = ProductionIndicator;
        ProductionGroupGOs[i] = ProductionGO;
        ProductionGroups[i] = ProductionGroup;

        Profiler.EndSample();
        return Base;
    }

    private void AddVisualsForProduction(int i, Production Production)
    {
        Profiler.BeginSample("ProductionIndicator.AddVisualsForProduction");

        bool bIsEmpty = Production.IsEmpty();
        ProductionGroups[i].UpdateVisuals(Production);
        Indicators[i].gameObject.SetActive(!bIsEmpty);
        ProductionIndicators[i].SetActive(!bIsEmpty);
        ProductionGroupGOs[i].SetActive(!bIsEmpty);
        Profiler.EndSample();   
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
        Profiler.BeginSample("ProductionIndicator.UpdateIndicatorVisuals");
        if (Indicators[i] == null)
            return;

        AddVisualsForProduction(i, NeighbourProductions[i]);
        Profiler.EndSample();
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

        return !HoveredHex.Location.Equals(LastLocation);
    }

    public override void UpdateIndicatorVisuals()
    {
        CalculateNeighbourProduction();
        base.UpdateIndicatorVisuals();
    }

    public override void CreateVisuals()
    {
        CalculateNeighbourProduction();
        base.CreateVisuals();
    }

    private void CalculateNeighbourProduction()
    {
        if (!Game.TryGetService(out MapGenerator MapGenerator))
            return;

        if (!TryGetBuildingInfo(out var Building, out var Location, out var Range))
            return;

        if (!Building.TryGetAdjacencyBonus(out var Bonuss))
            return;

        var Neighbours = MapGenerator.GetNeighboursDataArray(Location, Range == 0, Range);

        int Worker = Building.GetMaximumWorkerCount();
        for (int i = 0; i < Neighbours.Length; i++)
        {
            // eg map border
            if (Neighbours[i] == null)
            {
                NeighbourProductions[i] = Production.Empty;
                continue;
            }

            float Multiplier = AttributeSet.Get()[AttributeType.ProductionRate].GetAt(Neighbours[i].Location);
            bool bIsValid = Bonuss.TryGetValue(Neighbours[i].Type, out var NeighbourProduction);
            NeighbourProductions[i] = bIsValid ? Multiplier * NeighbourProduction * Worker : Production.Empty;
        }
    }
}
