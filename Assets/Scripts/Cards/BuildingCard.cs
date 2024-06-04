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

        GameObject Production = IconFactory.GetVisualsForProduction(BuildingData.Cost, this);
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
            MessageSystem.CreateMessage(Message.Type.Warning, "A card has been lost due to durability");
            bWasUsedUp = true;
        }
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
        if (!Game.TryGetServices(out MapGenerator Generator, out Stockpile Stockpile))
            return false;

        if (Generator.IsBuildingAt(Hex.Location))
        {
            MessageSystem.CreateMessage(Message.Type.Error, "Cannot create building here - one already exists");
            return false;
        }

        if (!BuildingData.CanBeBuildOn(Hex, false))
        {
            MessageSystem.CreateMessage(Message.Type.Error, "Cannot create building here - invalid placement");
            return false;
        }

        if (!Stockpile.CanAfford(BuildingData.GetCosts()))
        {
            MessageSystem.CreateMessage(Message.Type.Error, "Cannot create building here - not enough resources");
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
            MessageSystem.CreateMessage(Message.Type.Error, "Cannot create building here - not enough resources");
            return;
        }

        BuildingData.BuildAt(Hex.Location);
        Selector.ForceDeselect();
        Selector.SelectHexagon(Hex);

        Use();
    }

    public void RefreshUsage()
    {
        BuildingData.CurrentUsages = BuildingData.MaxUsages;
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
        return GetBuildingData().CanBeBuildOn(Hex, false);
    }

    public override bool IsCustomRuleApplying(Location NeighbourLocation)
    {
        if (!Game.TryGetService(out MapGenerator MapGenerator))
            return false;

        return MapGenerator.IsBuildingAt(NeighbourLocation) && GetBuildingData().IsNeighbourBuildingBlocking();
    }

    protected BuildingData BuildingData;
    protected RectTransform MaxWorkerTransform;
}
