using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static TutorialSystem;

public class TutorialScoutMultiQuest : MultiQuest<BuildingData, TokenizedUnitData>
{
    public override bool AreRequirementsFulfilled()
    {
        return true;
    }

    public override System.Type GetQuest1Type()
    {
        return typeof(TutorialScoutBuildingQuest);
    }

    public override System.Type GetQuest2Type()
    {
        return typeof(TutorialScoutUnitQuest);
    }

    public override bool IsQuest2Cancel()
    {
        return false;
    }

    public override bool ShouldUnlockDirectly()
    {
        return true;
    }

    public override void OnCreated()
    {
        if (!Game.TryGetService(out TutorialSystem TutorialSystem))
            return;
        if (!Game.TryGetService(out CardFactory CardFactory))
            return;

        CardFactory.CreateCard(BuildingConfig.Type.ForagersHut, 0, null, AddCard);
        CardFactory.CreateCard(UnitData.UnitType.Scout, 0, null, AddScoutCard);
        TutorialSystem.DisplayTextFor(TutorialType.Scouts);
    }

    private void AddScoutCard(Card Card)
    {
        EventCard ECard = Card as EventCard;
        GrantUnitEventData EData = ECard.EventData as GrantUnitEventData;
        EData.bIsTemporary = false;
        AddCard(Card);
    }

    private void AddCard(Card Card)
    {
        if (!Game.TryGetService(out CardHand CardHand))
            return;

        CardHand.AddCard(Card);
    }

    public override bool TryGetNextType(out System.Type Type)
    {
        Type = typeof(TutorialMalaiseQuest);
        return true;
    }
}

public class TutorialScoutBuildingQuest : Quest<BuildingData>
{
    public TutorialScoutBuildingQuest() : base()
    {
    }

    public override int CheckSuccess(BuildingData Target)
    {
        return Target.BuildingType == BuildingConfig.Type.ForagersHut ? 1 : 0;
    }

    public override string GetDescription()
    {
        return "Build and staff a foragers hut, recruit and move the scout";
    }

    public override int GetMaxProgress()
    {
        return 1;
    }

    public override Type GetQuestType()
    {
        return Type.Positive;
    }

    public override Dictionary<IQuestRegister<BuildingData>, ActionList<BuildingData>> GetRegisterMap()
    {
        if (Game.GetService<BuildingService>() == null)
            return new();

        return new()
        {
            { Game.GetService<BuildingService>(), BuildingService._OnBuildingBuilt }
        };
    }

    public override Sprite GetSprite()
    {
        if (!Game.TryGetService(out IconFactory IconFactory))
            return null;

        return IconFactory.GetIconForMisc(IconFactory.MiscellaneousType.Buildings);
    }

    public override int GetStartProgress()
    {
        return 0;
    }

    public override void OnAfterCompletion() { }

    public override void OnCreated(){}

    public override bool AreRequirementsFulfilled()
    {
        return true;
    }

    public override bool TryGetNextType(out System.Type Type)
    {
        Type = default;
        return false;
    }

    public override void GrantRewards() {}
}


public class TutorialScoutUnitQuest : Quest<TokenizedUnitData>
{
    public TutorialScoutUnitQuest() : base() {}

    public override int CheckSuccess(TokenizedUnitData Target)
    {
        return Target.Type == UnitData.UnitType.Scout ? 1 : 0;
    }

    public override string GetDescription()
    {
        return "Build and staff a foragers hut, recruit and move the scout";
    }

    public override int GetMaxProgress()
    {
        return 1;
    }

    public override Type GetQuestType()
    {
        return Type.Positive;
    }

    public override Dictionary<IQuestRegister<TokenizedUnitData>, ActionList<TokenizedUnitData>> GetRegisterMap()
    {
        if (Game.GetService<Units>() == null)
            return new();

        return new()
        {
            { Game.GetService<Units>(), Units._OnUnitMoved }
        };
    }

    public override Sprite GetSprite()
    {
        if (!Game.TryGetService(out IconFactory IconFactory))
            return null;

        return IconFactory.GetIconForMisc(IconFactory.MiscellaneousType.Scout);
    }

    public override int GetStartProgress()
    {
        return 0;
    }

    public override void OnAfterCompletion() { }

    public override void OnCreated() { }

    public override bool AreRequirementsFulfilled()
    {
        return true;
    }

    public override bool TryGetNextType(out System.Type Type)
    {
        Type = default;
        return false;
    }
    public override void GrantRewards() { }
}

