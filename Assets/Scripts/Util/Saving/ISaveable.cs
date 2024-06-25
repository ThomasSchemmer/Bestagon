
using System;
using Unity.Collections;
using UnityEngine;

/** 
 * Provides access to save game information, such as id, size and data
 */
public interface ISaveable
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
        Units = 10
    }

    public static int MaxTypeIndex = 8;

    public abstract int GetSize();

    public static int GetStaticSize() { 
        // Not everything can actually be statically computed! if this exception gets thrown,
        // you need to either implement one if possible or use GetSize()
        throw new NotImplementedException();
    }

    /** Overwrite if the object should be loaded with a saved size 
     * Necessary eg when the overall size changes each play
     * Assumes that the first int in the saveable is the byte-size!
     */
    public bool ShouldLoadWithLoadedSize() { return false; }

    public abstract byte[] GetData();

    public abstract void SetData(NativeArray<byte> Bytes);

    /** Overwrite for actions that have to be taken after loading the data */
    public void Load() { }
}
