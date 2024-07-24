using System;
using System.Collections.Generic;
using System.Diagnostics;
using static BuildingService;

/** 
 * Provides unified access to all of the different quest conditions.
 * Used to register (aka tell the quest on what to trigger) and deregister (aka 
 * prohibit calling a completed quest)
 * Since quests can require different event triggers for the same generic type 
 * (see @BuildBuildings and @MainQuestMalaise) the delegate needs to be in this interface too
 */
public interface IQuestRegister {}

public interface IQuestRegister<T> : IQuestRegister
{
    public void RegisterQuest(ActionList<T> Delegates, Quest<T> Quest)
    {
        Delegates.Add(Quest.OnQuestProgress);
    }

    public void DeRegisterQuest(ActionList<T> Delegates, Quest<T> Quest)
    {
        Delegates.Remove(Quest.OnQuestProgress);
    }
}
