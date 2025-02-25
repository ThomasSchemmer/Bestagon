using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static IconFactory;

public class LibraryResearchScreenFeature : ScreenFeature<HexagonData>
{
    public RectTransform CostTransform;
    public TMPro.TextMeshProUGUI FallbackText;
    public TMPro.TextMeshProUGUI InfoText;

    public override bool ShouldBeDisplayed()
    {
        return TryGetTargetLibrary(out var _);
    }

    private bool TryGetTargetLibrary(out BuildingEntity Library)
    {
        Library = null;
        if (!Game.TryGetService(out BuildingService Buildings))
            return false;

        HexagonData Hex = Target.GetFeatureObject();
        if (Hex == null)
            return false;

        if (!Buildings.TryGetEntityAt(Hex.Location, out var Building))
            return false;

        if (Building.BuildingType != BuildingConfig.Type.Library)
            return false;

        Library = Building;
        return true;
    }


    public override void ShowAt(float YOffset, float Height)
    {
        base.ShowAt(YOffset, Height);

        ApplyCostVisuals();
        bool bShowFallback = ShouldFallbackBeDisplayed();
        if (bShowFallback)
        {
            ShowFallback();
        }
        else
        {
            ShowButton();
        }
    }

    private void Cleanup()
    {
        if (CostTransform.childCount == 0)
            return;

        for (int i = CostTransform.childCount - 1; i >= 0; i--)
        {
            Destroy(CostTransform.GetChild(i).gameObject);
        }
    }

    private void ShowButton()
    {
        InfoText.gameObject.SetActive(true);
        FallbackText.gameObject.SetActive(false);

        if (!Game.TryGetService(out AmberService Ambers))
            return;

        InfoText.text = Ambers.ResearchTurns + "/" + AmberService.MaxResearchTurnAmount + FlavourText;
    }

    private void ShowFallback()
    {
        InfoText.gameObject.SetActive(false);
        FallbackText.gameObject.SetActive(true);

        bool bAreWorkersAssigned = AreEnoughWorkersAssigned();
        bool bAreWorkersStarving = AreAssignedWorkersStarving();
        bool bHasEnoughResources = HasEnoughResources();
        bool bIsUnlocked = IsAlreadyUnlocked();
        string TargetText =
            bIsUnlocked ? IdleText : 
            !bAreWorkersAssigned ? ProductionScreenFeature.NoWorkersText :
            bAreWorkersStarving ? ProductionScreenFeature.StarvingWorkersText :
            !bHasEnoughResources ? ProductionScreenFeature.NoConsumptionText : UnknownText;

        FallbackText.text = TargetText;
    }

    private void ApplyCostVisuals()
    {
        Cleanup();
        if (!TryGetTargetLibrary(out BuildingEntity BuildingData))
            return;

        Game.TryGetService(out IconFactory IconFactory);

        GameObject CostVisuals = IconFactory.GetVisualsForProduction(BuildingData.Effect.Consumption, null, true).gameObject;

        CostVisuals.transform.SetParent(CostTransform, false);
    }

    private bool IsAlreadyUnlocked()
    {
        if (!Game.TryGetService(out AmberService Ambers))
            return false;

        return Ambers.IsUnlocked();
    }

    private bool HasEnoughResources()
    {
        if (!TryGetTargetLibrary(out BuildingEntity Building))
            return false;

        if (!Game.TryGetService(out Stockpile Stockpile))
            return false;

        return Stockpile.CanAfford(Building.Effect.Consumption, true);
    }


    private bool AreEnoughWorkersAssigned()
    {
        if (!TryGetTargetLibrary(out BuildingEntity Building))
            return false;

        switch (Building.Effect.EffectType)
        {
            case OnTurnBuildingEffect.Type.Library: return Building.GetAssignedWorkerCount() == Building.GetMaximumWorkerCount();
        }
        return false;
    }

    private bool AreAssignedWorkersStarving()
    {
        return TryGetTargetLibrary(out BuildingEntity Building) &&
            Building.GetWorkingWorkerCount(true) < Building.GetAssignedWorkerCount();
    }


    private bool ShouldFallbackBeDisplayed()
    {
        if (!TryGetTargetLibrary(out BuildingEntity Library))
            return false;

        Library.SimulateCurrentFood();

        return !Library.Effect.CanResearchInLibrary(true);
    }

    public override void Hide()
    {
        base.Hide();
        InfoText.gameObject.SetActive(false);
        FallbackText.gameObject.SetActive(false);
    }

    private static string IdleText = "Nothing to research. Collect more Ambers";
    private static string UnknownText = "Unknown error";
    private static string FlavourText = " turns researched";
}
