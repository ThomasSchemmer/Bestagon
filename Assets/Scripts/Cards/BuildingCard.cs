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
        EffectTransform = transform.Find("Effects").GetComponent<RectTransform>();
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

        GameObject Usages = IconFactory.GetVisualsForMiscalleneous(IconFactory.MiscellaneousType.Usages, BuildingData.CurrentUsages);
        Usages.transform.SetParent(UsagesTransform, false);

        GameObject Icons = IconFactory.GetVisualsForProduction(BuildingData.Cost);
        Icons.transform.SetParent(CostTransform, false);

        GameObject MaxWorker = IconFactory.GetVisualsForMiscalleneous(IconFactory.MiscellaneousType.Worker, BuildingData.MaxWorker);
        MaxWorker.transform.SetParent(MaxWorkerTransform, false);

        GameObject EffectObject = BuildingData.Effect.GetEffectVisuals();
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

    protected override CardCollection GetUseTarget()
    {
        if (!Game.TryGetServices(out CardStash CardStash, out CardDeck CardDeck))
            return null;

        bool bIsUsedUp = BuildingData.CurrentUsages <= 0;
        CardCollection Target = bIsUsedUp ? CardStash : CardDeck;
        return Target;
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

    protected BuildingData BuildingData;
    protected RectTransform MaxWorkerTransform, EffectTransform;
}
