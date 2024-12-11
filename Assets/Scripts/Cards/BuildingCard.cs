using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VectorGraphics;
using UnityEngine;

public class BuildingCard : Card
{
    public void Init(BuildingEntity BuildingData, CardDTO DTO, int Index)
    {
        // needs to be set before initialising the base
        this.BuildingData = BuildingData;
        base.Init(DTO, Index);
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
        DeleteVisuals(SymbolTransform);
    }

    public override void Show(Visibility Visibility)
    {
        base.Show(Visibility);
        MaxWorkerTransform.gameObject.SetActive(Visibility >= Visibility.Visible);
    }

    protected override void GenerateVisuals()
    {
        base.GenerateVisuals();

        if (!Game.TryGetService(out IconFactory IconFactory))
            return;

        GameObject Usages = IconFactory.GetVisualsForMiscalleneous(IconFactory.MiscellaneousType.Usages, this, BuildingData.CurrentUsages);
        Usages.transform.SetParent(UsagesTransform, false);

        GameObject Production = IconFactory.GetVisualsForProduction(BuildingData.GetCosts(), this, true);
        Production.transform.SetParent(CostTransform, false);

        GameObject MaxWorker = IconFactory.GetVisualsForMiscalleneous(IconFactory.MiscellaneousType.Worker, this, BuildingData.GetMaximumWorkerCount());
        MaxWorker.transform.SetParent(MaxWorkerTransform, false);

        GameObject EffectObject = BuildingData.Effect.GetEffectVisuals(this);
        EffectObject.transform.SetParent(EffectTransform, false);

        SVGImage Image = SymbolTransform.GetComponent<SVGImage>();
        Image.sprite = IconFactory.GetIconForMisc(GetMiscBuildingSize());
        Image.color = new(1, 1, 1, 1);
    }

    private IconFactory.MiscellaneousType GetMiscBuildingSize()
    {
        switch (BuildingData.Area)
        {
            case LocationSet.AreaSize.Single: return IconFactory.MiscellaneousType.SingleTile;
            case LocationSet.AreaSize.Double: return IconFactory.MiscellaneousType.DoubleTile;
            case LocationSet.AreaSize.TripleLine: return IconFactory.MiscellaneousType.TripleLineTile;
            case LocationSet.AreaSize.TripleCircle: return IconFactory.MiscellaneousType.TripleCircleTile;
        }
        return default;
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


    public Transform GetBuildableOnTransform()
    {
        if (transform.childCount < 7)
            return null;

        Transform Temp = transform.GetChild(6);
        if (Temp.childCount < 1)
            return null;

        Temp = Temp.GetChild(0);
        int BuildableOnIndex = BuildingData.Effect.EffectType == OnTurnBuildingEffect.Type.ConsumeProduce ? 5 : 3;
        if (Temp.childCount < BuildableOnIndex + 1)
            return null;

        return Temp.GetChild(BuildableOnIndex);
    }

    protected override CardCollection GetTargetAfterUse()
    {
        if (!Game.TryGetServices(out CardStash CardStash, out DiscardDeck DiscardDeck))
            return null;

        bool bIsUsedUp = BuildingData.CurrentUsages <= 0;
        CardCollection Target = bIsUsedUp ? CardStash : DiscardDeck;
        return Target;
    }

    public override bool IsCardInteractableWith(HexagonVisualization Hex)
    {
        if (!Game.TryGetServices(out BuildingService Buildings, out Stockpile Stockpile))
            return false;

        string Reason = "Cannot create building here - ";
        if (Buildings.TryGetEntityAt(Hex.Location, out var _))
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

        if (!LocationSet.TryGetAround(Hex.Location, BuildingData.Area, out LocationSet NewLocation))
        {
            MessageSystemScreen.CreateMessage(Message.Type.Error, "Cannot create building here - not enough space for its size");
            return;
        }

        // avoids "doubling" the actual building (including workers) when building a second time
        BuildingEntity Copy = Instantiate(BuildingData);

        Copy.BuildAt(NewLocation, LocationSet.GetAngle());
        Selector.ForceDeselect();
        Selector.SelectHexagon(Hex);

        Use();
    }

    public void RefreshUsage()
    {
        BuildingData.RefreshUsage();
        UpdateUsageText();
    }

    public void SetBuildingData(BuildingEntity BuildingData)
    {
        this.BuildingData = BuildingData;
    }

    public BuildingEntity GetBuildingData()
    {
        return BuildingData;
    }

    public BuildingEntity GetDTOData()
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

    public override LocationSet.AreaSize GetAreaSize()
    {
        return GetBuildingData().Area;
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

    public override bool CanBeUpgraded()
    {
        return BuildingData != null && BuildingData.IsAnyUpgradePossible();
    }

    protected BuildingEntity BuildingData;
    protected RectTransform MaxWorkerTransform;
}
