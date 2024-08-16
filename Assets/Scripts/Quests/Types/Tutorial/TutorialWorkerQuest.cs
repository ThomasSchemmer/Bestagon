using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static TutorialSystem;

public class TutorialWorkerQuest : Quest<WorkerData>
{
    public TutorialWorkerQuest() : base()
    {
    }

    public override int CheckSuccess(WorkerData Target)
    {
        return 1;
    }

    public override string GetDescription()
    {
        return "Create the worker and assign it to the claypit";
    }

    public override int GetMaxProgress()
    {
        return 1;
    }

    public override Type GetQuestType()
    {
        return Type.Positive;
    }

    public override Dictionary<IQuestRegister<WorkerData>, ActionList<WorkerData>> GetRegisterMap()
    {
        if (Game.GetService<Workers>() == null)
            return new();

        return new()
        {
            { Game.GetService<Workers>(), Workers._OnWorkerAssignedList }
        };
    }

    public override Sprite GetSprite()
    {
        if (!Game.TryGetService(out IconFactory IconFactory))
            return null;

        return IconFactory.GetIconForMisc(IconFactory.MiscellaneousType.Worker);
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

        CardFactory.CreateCard(EventData.EventType.GrantUnit, 0, null, AddCard);

        TutorialSystem.DisplayTextFor(TutorialType.Workers);
    }

    private void AddCard(Card Card)
    {
        if (!Game.TryGetService(out CardHand CardHand))
            return;

        if (Card is not EventCard)
            return;

        EventCard ECard = Card as EventCard;
        if (ECard.EventData is not GrantUnitEventData)
            return;

        GrantUnitEventData EventData = (GrantUnitEventData)ECard.EventData;
        EventData.GrantedType = UnitData.UnitType.Worker;
        Card.GenerateCard();

        CardHand.AddCard(Card);
    }

    public override bool AreRequirementsFulfilled()
    {
        return true;
    }

    public override bool TryGetNextType(out System.Type Type)
    {
        Type = typeof(TutorialTurnsQuest);
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
