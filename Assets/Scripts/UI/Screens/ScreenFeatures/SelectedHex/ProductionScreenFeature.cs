using TMPro;
using UnityEngine;
using static IconFactory;

/** 
 * Provides information about the production of the currently selected building,
 * or additional errors regarding workers etc
 * TODO: ineffecient multiple checks for production, should be triggered once and cashed instead
 */
public class ProductionScreenFeature : ScreenFeature<HexagonData>
{
    private TextMeshProUGUI FallbackText;
    private RectTransform ProductionTransform;

    public override void Init(ScreenFeatureGroup<HexagonData> Target)
    {
        base.Init(Target);
        ProductionTransform = transform.GetChild(0).GetComponent<RectTransform>();
        FallbackText = transform.GetChild(1).GetComponent<TextMeshProUGUI>();
    }

    public override bool ShouldBeDisplayed()
    {
        return TryGetBuildingData(out BuildingEntity Building) &&
            Building.Effect.EffectType != OnTurnBuildingEffect.Type.Merchant &&
            Building.Effect.EffectType != OnTurnBuildingEffect.Type.Library;
    }

    private bool ShouldFallbackBeDisplayed()
    {
        if (!TryGetBuildingData(out BuildingEntity Building))
            return false;

        Building.SimulateCurrentFood();

        switch (Building.Effect.EffectType)
        {
            case OnTurnBuildingEffect.Type.Merchant: return false;
            case OnTurnBuildingEffect.Type.ProduceUnit: return !Building.Effect.CanProduceUnit(true);
            case OnTurnBuildingEffect.Type.Produce: return Building.GetWorkingWorkerCount(true) == 0;
            case OnTurnBuildingEffect.Type.ConsumeProduce: return 
                    Building.GetWorkingWorkerCount(true) < Building.GetMaximumWorkerCount() ||
                    !AreEnoughResourcesAvailable();
            default: return true;
        }
    }

    private bool AreAssignedWorkersStarving()
    {
        return TryGetBuildingData(out BuildingEntity Building) &&
            Building.GetWorkingWorkerCount(true) == 0 &&
            Building.GetAssignedWorkerCount() > 0;
    }

    private bool AreEnoughWorkersAssigned()
    {
        if (!TryGetBuildingData(out BuildingEntity Building))
            return false;

        switch (Building.Effect.EffectType)
        {
            case OnTurnBuildingEffect.Type.ConsumeProduce: // intentional fallthrough
            case OnTurnBuildingEffect.Type.Merchant:
            case OnTurnBuildingEffect.Type.Produce: return Building.GetAssignedWorkerCount() > 0;
            case OnTurnBuildingEffect.Type.ProduceUnit: return Building.GetAssignedWorkerCount() == Building.GetMaximumWorkerCount();
        }
        return false;
    }

    private bool TryGetBuildingData(out BuildingEntity Building)
    {
        Building = null;
        if (!Game.TryGetServices(out IconFactory IconFactory, out BuildingService Buildings))
            return false;

        HexagonData SelectedHex = Target.GetFeatureObject();
        if (SelectedHex == null || !Buildings.TryGetEntityAt(SelectedHex.Location, out Building))
            return false;

        return true;
    }

    public override void ShowAt(float YOffset, float Height)
    {
        base.ShowAt(YOffset, Height);

        ProductionTransform.sizeDelta = new(ProductionTransform.sizeDelta.x, Height);

        bool bShowFallback = ShouldFallbackBeDisplayed();

        ShowProduction(bShowFallback);
        if (bShowFallback)
        {
            ShowFallback();
        }
    }

    private void ShowProduction(bool bShowFallback)
    {
        FallbackText.gameObject.SetActive(false);
        ProductionTransform.gameObject.SetActive(true);
        Cleanup();

        TryGetBuildingData(out BuildingEntity BuildingData);
        if (BuildingData.Effect.EffectType != OnTurnBuildingEffect.Type.ProduceUnit)
        {
            ApplyProductionVisuals(bShowFallback);
        }
        else
        {
            ApplyUnitVisuals(bShowFallback);
        }
    }

    public override float GetHeight()
    {
        int Multiplier = ShouldFallbackBeDisplayed() ? 2 : 1;
        return Height * Multiplier;
    }

    private void ApplyUnitVisuals(bool bShowFallback)
    {
        TryGetBuildingData(out BuildingEntity BuildingData);
        Game.TryGetService(out IconFactory IconFactory);

        var Type = BuildingData.Effect.UnitType;

        if (!IconFactory.TryGetMiscFromUnit(Type, out MiscellaneousType UnitType))
            return;

        GameObject UnitVisuals = IconFactory.GetVisualsForMiscalleneous(UnitType, null, 1);
        GameObject CostVisuals = IconFactory.GetVisualsForProduction(BuildingData.Effect.Consumption, null, true).gameObject;

        float yOffset = bShowFallback ? Height / 2f : 0;
        UnitVisuals.transform.SetParent(ProductionTransform, false);
        UnitVisuals.GetComponent<RectTransform>().anchoredPosition = new Vector2(30, yOffset);
        CostVisuals.transform.SetParent(ProductionTransform, false);
        CostVisuals.GetComponent<RectTransform>().anchoredPosition = new Vector2(90, yOffset);
    }

    private void ApplyProductionVisuals(bool bShowFallback)
    {
        TryGetBuildingData(out BuildingEntity BuildingData);
        Game.TryGetService(out IconFactory IconFactory);

        Production Production = bShowFallback ?
                BuildingData.GetTheoreticalMaximumProduction() :
                BuildingData.GetProduction(true);
        GameObject Visuals = IconFactory.GetVisualsForProduction(Production, null, false).gameObject;
        Visuals.transform.SetParent(ProductionTransform, false);

        RectTransform VisRect = Visuals.GetComponent<RectTransform>();
        VisRect.anchoredPosition = new(VisRect.anchoredPosition.x, bShowFallback ? Height / 2f : 0);
    }

    private void ShowFallback()
    {
        FallbackText.gameObject.SetActive(true);

        bool bAreWorkersAssigned = AreEnoughWorkersAssigned();
        bool bAreWorkersStarving = AreAssignedWorkersStarving();
        bool bHaveEnoughResources = AreEnoughResourcesAvailable();
        string TargetText =
            !bAreWorkersAssigned ? NoWorkersText :
            bAreWorkersStarving ? StarvingWorkersText :
            !bHaveEnoughResources ? NoConsumptionText : "Unknown error";

        FallbackText.text = TargetText;
        FallbackText.color = bAreWorkersStarving ? StarvingWorkersColor : NoWorkersColor;

        FallbackText.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -Height / 2f);
    }

    private bool AreEnoughResourcesAvailable()
    {

        if (!TryGetBuildingData(out BuildingEntity Building))
            return true;

        switch (Building.Effect.EffectType)
        {
            case OnTurnBuildingEffect.Type.ConsumeProduce: return !Building.GetProduction(true).IsEmpty();
            case OnTurnBuildingEffect.Type.ProduceUnit: return Building.Effect.CanProduceUnit(true);
            default:return true;
        }
    }

    private void Cleanup()
    {
        if (ProductionTransform.childCount == 0)
            return;

        for (int i = ProductionTransform.childCount - 1; i >= 0; i--)
        {
            Destroy(ProductionTransform.GetChild(i).gameObject);
        }
    }


    public override void Hide()
    {
        base.Hide();

        FallbackText.gameObject.SetActive(false);
        ProductionTransform.gameObject.SetActive(false);
    }

    public static string NoConsumptionText = "Not enough resources to work!";
    public static string NoWorkersText = "Assign more workers!";
    public static string StarvingWorkersText = "Workers are hungry and refuse to work!";

    private static Color NoWorkersColor = Color.yellow;
    private static Color StarvingWorkersColor = Color.red;

    private static float Height = 35;
}
