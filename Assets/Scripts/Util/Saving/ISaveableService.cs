using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/** 
 * Provides access to save data from services and cleanly load them even while the game
 * is already running
 */ 
public interface ISaveableService : ISaveableData
{
    public enum SaveGameType
    {
        None = 0,
        MapGenerator = 1,
        CardHand = 2,
        CardDeck = 3,
        CardStash = 4,
        Unlockables = 5,
        Stockpile = 6,
        Statistics = 7,
        Buildings = 8,
        Workers = 9,
        Units = 10,
        Malaise = 11,
        Spawning = 12,
        Quests = 13
    }

    /** Convenience function, should be instead the same class */
    public bool IsServiceInit()
    {
        if (this is not GameService)
            return false;

        GameService Service = this as GameService;
        if (Service == null)
            return false;

        return Service.IsInit;
    }

    /** Deletes all internal data so that it can be loaded from the save or initialized safely*/
    public void Reset();

    /** Overwrite for actions that have to be taken after loading the data */
    public void Load() { }
}
