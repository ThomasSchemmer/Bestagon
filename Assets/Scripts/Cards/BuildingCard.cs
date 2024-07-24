using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildingCard : Card
{
    public void Init(BuildingData BuildingData, int Index)
    {
        // needs to be set before initialising the base
        this.BuildingData = BuildingData;
        base.Init(Index);
        this.BuildingData.Init();
    }

    public override string GetName()
    {
        return BuildingData.BuildingType.ToString();
    }

    protected override void LinkTexts()
    {
        base.LinkTexts();
        MaxWorkerTransform = transform.Find("MaxWorker").GetComponent<RectTransform>();
    }

    public RectTransform GetMaxWorkerTransform()
    {
        return MaxWorkerTransform;
    }

    protected override void DeleteVisuals()
    {
        base.DeleteVisuals();
        DeleteVisuals(MaxWorkerTransform);
        DeleteVisuals(EffectTransform);
    }

    protected override void GenerateVisuals()
    {
        base.GenerateVisuals();

        if (!Game.TryGetService(out IconFactory IconFactory))
            return;

        GameObject Usages = IconFactory.GetVisualsForMiscalleneous(IconFactory.MiscellaneousType.Usages, this, BuildingData.CurrentUsages);
        Usages.transform.SetParent(UsagesTransform, false);

        GameObject Production = IconFactory.GetVisualsForProduction(BuildingData.Cost, this, true);
        Production.transform.SetParent(CostTransform, false);

        GameObject MaxWorker = IconFactory.GetVisualsForMiscalleneous(IconFactory.MiscellaneousType.Worker, this, BuildingData.MaxWorker);
        MaxWorker.transform.SetParent(MaxWorkerTransform, false);

        GameObject EffectObject = BuildingData.Effect.GetEffectVisuals(this);
        EffectObject.transform.SetParent(EffectTransform, false);
    }

    protected override void UseInternal()
    {
        BuildingData.CurrentUsages--;

        bool bIsUsedUp = BuildingData.CurrentUsages <= 0;
        if (bIsUsedUp)
        {
            MessageSystemScreen.CreateMessage(Message.Type.Warning, "A "+BuildingData.BuildingType.ToString()+" card has been lost due to durability");
            bWasUsedUp = true;
        }
        UpdateUsageText();
    }

    private void UpdateUsageText()
    {
        NumberedIconScreen Screen = UsagesTransform.GetChild(0).gameObject.GetComponent<NumberedIconScreen>();
        Screen.UpdateVisuals(BuildingData.CurrentUsages);
    }

    protected override CardCollection GetTargetAfterUse()
    {
        if (!Game.TryGetServices(out CardStash CardStash, out CardDeck CardDeck))
            return null;

        bool bIsUsedUp = BuildingData.CurrentUsages <= 0;
        CardCollection Target = bIsUsedUp ? CardStash : CardDeck;
        return Target;
    }

    public override bool IsCardInteractableWith(HexagonVisualization Hex)
    {
        if (!Game.TryGetServices(out BuildingService Buildings, out Stockpile Stockpile))
            return false;

        string Reason = "Cannot create building here - ";
        if (Buildings.IsBuildingAt(Hex.Location))
        {
            MessageSystemScreen.CreateMessage(Message.Type.Error, Reason + "one already exists");
            return false;
        }

        if (!BuildingData.CanBeBuildOn(Hex, false, out string Reason2))
        {
            MessageSystemScreen.CreateMessage(Message.Type.Error, Reason + Reason2);
            return false;
        }

        if (!Stockpile.CanAfford(BuildingData.GetCosts()))
        {
            MessageSystemScreen.CreateMessage(Message.Type.Error, Reason + "not enough resources");
            return false;
        }
        return true;
    }

    public override void InteractWith(HexagonVisualization Hex)
    {
        if (!Game.TryGetServices(out Stockpile Stockpile, out Selectors Selector))
            return;

        if (!Stockpile.Pay(BuildingData.GetCosts()))
        {
            MessageSystemScreen.CreateMessage(Message.Type.Error, "Cannot create building here - not enough resources");
            return;
        }

        // avoids "doubling" the actual building (including workers) when building a second time
        BuildingData Copy = Instantiate(BuildingData);
        Copy.BuildAt(Hex.Location);
        Selector.ForceDeselect();
        Selector.SelectHexagon(Hex);

        Use();
    }

    public void RefreshUsage()
    {
        BuildingData.CurrentUsages = BuildingData.MaxUsages;
        UpdateUsageText();
    }

    public void SetBuildingData(BuildingData BuildingData)
    {
        this.BuildingData = BuildingData;
    }

    public BuildingData GetBuildingData()
    {
        return BuildingData;
    }

    public BuildingData GetDTOData()
    {
        return BuildingData;
    }

    public override bool IsPreviewable()
    {
        return BuildingData.BuildingType != BuildingConfig.Type.DEFAULT;
    }

    public override int GetAdjacencyRange()
    {
        return GetBuildingData().Effect.Range;
    }

    public override bool TryGetAdjacencyBonus(out Dictionary<HexagonConfig.HexagonType, Production> Bonus)
    {
        return GetBuildingData().TryGetAdjacencyBonus(out Bonus);
    }

    public override bool ShouldShowAdjacency(HexagonVisualization Hex)
    {
        return GetBuildingData().CanBeBuildOn(Hex, false, out string _);
    }

    public override bool IsCustomRuleApplying(Location NeighbourLocation)
    {
        return false;
    }

    protected BuildingData BuildingData;
    protected RectTransform MaxWorkerTransform;
}
