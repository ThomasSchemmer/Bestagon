
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
        None = -1,
        MapGenerator = 0,
        CardHand = 1,
        CardDeck = 2,
        CardStash = 3,
        Unlockables = 4,
        Stockpile = 5
    }

    public static int MaxTypeIndex = 5;

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
