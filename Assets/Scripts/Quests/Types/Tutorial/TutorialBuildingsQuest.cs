using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static TutorialSystem;

public class TutorialBuildingsQuest : Quest<BuildingEntity>
{
    public TutorialBuildingsQuest() : base()
    {
    }

    public override int CheckSuccess(BuildingEntity Target)
    {
        return 1;
    }

    public override string GetDescription()
    {
        return "Build the claypit";
    }

    public override int GetMaxProgress()
    {
        return 1;
    }

    public override Type GetQuestType()
    {
        return Type.Positive;
    }

    public override Dictionary<IQuestRegister<BuildingEntity>, ActionList<BuildingEntity>> GetRegisterMap()
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

    public override void OnCreated()
    {
        if (!Game.TryGetService(out TutorialSystem TutorialSystem))
            return;
        if (!Game.TryGetService(out CardFactory CardFactory))
            return;

        CardFactory.CreateCard(BuildingConfig.Type.Claypit, 0, null, AddCard);

        TutorialSystem.DisplayTextFor(TutorialType.Buildings);
    }

    private void AddCard(Card Card)
    {
        if (!Game.TryGetService(out CardHand CardHand))
            return;

        CardHand.AddCard(Card);
    }

    public override bool AreRequirementsFulfilled()
    {
        return true;
    }

    public override bool TryGetNextType(out System.Type Type)
    {
        Type = typeof(TutorialWorkerQuest);
        return true;
    }

    public override bool ShouldUnlockDirectly()
    {
        return true;
    }

    public override void GrantRewards()
    {
    }
}
