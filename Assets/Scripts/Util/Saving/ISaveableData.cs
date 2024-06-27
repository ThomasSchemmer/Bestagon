
using System;
using Unity.Collections;
using UnityEngine;

/** 
 * Provides access to save game information, such as id, size and data
 * Should be used for saved data types, not for services (which might store a collection of Data's)
 * See @ISaveableService for that
 */
public interface ISaveableData
{
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
}
