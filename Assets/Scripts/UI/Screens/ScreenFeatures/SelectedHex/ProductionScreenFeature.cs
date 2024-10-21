using TMPro;
using UnityEngine;

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
        return TryGetBuildingData(out BuildingEntity Building) && Building.Effect.EffectType != OnTurnBuildingEffect.Type.Merchant;
    }

    private bool ShouldProductionBeDisplayed()
    {
        if (!TryGetBuildingData(out BuildingEntity Building))
            return false;

        Building.SimulateCurrentFood();

        switch (Building.Effect.EffectType)
        {
            case OnTurnBuildingEffect.Type.Merchant: return false;
            case OnTurnBuildingEffect.Type.ProduceUnit: return false;
            case OnTurnBuildingEffect.Type.Produce: 
            case OnTurnBuildingEffect.Type.ConsumeProduce: return Building.GetWorkingWorkerCount(true) > 0 && AreEnoughResourcesAvailable();
            default: return false;
        }
    }

    private bool AreAssignedWorkersStarving()
    {
        return TryGetBuildingData(out BuildingEntity Building) &&
            Building.GetWorkingWorkerCount(true) == 0 &&
            Building.GetAssignedWorkerCount() > 0;
    }

    private bool AreWorkersAssigned()
    {
        return TryGetBuildingData(out BuildingEntity Building) &&
            Building.GetWorkingWorkerCount(true) > 0;
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

    public override void ShowAt(float YOffset)
    {
        base.ShowAt(YOffset);
        if (ShouldProductionBeDisplayed())
        {
            ShowProduction();
        }
        else
        {
            ShowFallback();
        }
    }

    private void ShowProduction()
    {
        TryGetBuildingData(out BuildingEntity BuildingData);
        Game.TryGetService(out IconFactory IconFactory);

        FallbackText.gameObject.SetActive(false);
        ProductionTransform.gameObject.SetActive(true);
        Cleanup();

        Production Production = BuildingData.GetProduction(true);
        GameObject Visuals = IconFactory.GetVisualsForProduction(Production, null, false);
        Visuals.transform.SetParent(ProductionTransform, false);
    }

    private void ShowFallback()
    {
        bool bAreWorkersAssigned = AreWorkersAssigned();
        bool bAreWorkersStarving = AreAssignedWorkersStarving();
        bool bHaveEnoughResources = AreEnoughResourcesAvailable();
        FallbackText.gameObject.SetActive(true);
        string TargetText =
            !bAreWorkersAssigned ? NoWorkersText :
            bAreWorkersStarving ? StarvingWorkersText :
            !bHaveEnoughResources ? NoConsumptionText : "Unknown error";
        FallbackText.text = TargetText;
        FallbackText.color = bAreWorkersStarving ? StarvingWorkersColor : NoWorkersColor;

        ProductionTransform.gameObject.SetActive(false);
        Cleanup();
    }

    private bool AreEnoughResourcesAvailable()
    {

        if (!TryGetBuildingData(out BuildingEntity Building))
            return true;

        if (Building.Effect.EffectType != OnTurnBuildingEffect.Type.ConsumeProduce)
            return true;

        return !Building.GetProduction(true).IsEmpty();
    }

    private void Cleanup()
    {
        if (ProductionTransform.childCount == 0)
            return;

        Destroy(ProductionTransform.GetChild(0).gameObject);
    }


    public override void Hide()
    {
        base.Hide();

        FallbackText.gameObject.SetActive(false);
        ProductionTransform.gameObject.SetActive(false);
    }

    private static string NoConsumptionText = "Not enough resources to work!";
    private static string NoWorkersText = "No workers assigned!";
    private static string StarvingWorkersText = "Workers are hungry and refuse to work!";

    private static Color NoWorkersColor = Color.yellow;
    private static Color StarvingWorkersColor = Color.red;
}
