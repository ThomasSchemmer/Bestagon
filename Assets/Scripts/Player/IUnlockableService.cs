using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/** 
 * Allows for handling of @Unlockables through the containing service
 */
public interface IUnlockableService<T> where T : struct, IConvertible
{
    /** Returns true if the service is ready */
    public bool IsInit();
    /** Initializes the unlockables registry, filling up the data and resetting all states */
    public void InitUnlockables();

    /** Is called whenever an Unlockable has been loaded, to allow for callbacks*/
    public void OnLoadedUnlockable(T Type, Unlockables.State State);

    public void OnLoadedUnlockables();

    /** Allows type casting with templated values */
    public int GetValueAsInt(T Value);
    public T GetValueAsT(int Value);

    public T Combine(T A, T B);
}
