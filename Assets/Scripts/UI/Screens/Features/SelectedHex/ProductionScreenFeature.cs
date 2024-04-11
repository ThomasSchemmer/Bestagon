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
        return TryGetBuildingData(out BuildingData Building);
    }

    private bool ShouldProductionBeDisplayed()
    {
        return
            TryGetBuildingData(out BuildingData Building) &&
            Building.GetWorkingWorkerCount() > 0;
    }

    private bool TryGetBuildingData(out BuildingData Building)
    {
        Building = null;
        if (!Game.TryGetServices(out IconFactory IconFactory, out MapGenerator MapGenerator))
            return false;

        HexagonData SelectedHex = Target.GetFeatureObject();
        if (SelectedHex == null || !MapGenerator.TryGetBuildingAt(SelectedHex.Location, out Building))
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
        TryGetBuildingData(out BuildingData BuildingData);
        Game.TryGetService(out IconFactory IconFactory);

        FallbackText.gameObject.SetActive(false);
        ProductionTransform.gameObject.SetActive(true);
        Cleanup();

        Production Production = BuildingData.GetProduction();
        GameObject Visuals = IconFactory.GetVisualsForProduction(Production);
        Visuals.transform.SetParent(ProductionTransform, false);
    }

    private bool ShowFallback()
    {
        FallbackText.gameObject.SetActive(true);
        ProductionTransform.gameObject.SetActive(false);
        Cleanup();
        return true;
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
}
