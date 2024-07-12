using System;

/** 
 * Provides unified access to all of the different quest conditions.
 * Used to register (aka tell the quest on what to trigger) and deregister (aka 
 * prohibit calling a completed quest)
 * Since most callbacks are static, this sadly cannot be called on objects directly, adding another
 * layer of indirection
 * Does not need to be contained in the class T, see @Stockpile, which uses it for @Production
 */
public interface IQuestCompleter<T> 
{
    public static void RegisterQuest(Quest<T> Quest) => throw new NotImplementedException();

    public static void DeregisterQuest(Quest<T> Quest) => throw new NotImplementedException();
}
