using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/** Helper struct to allow modification of lists during runtime
 * regular list cannot be updated from itself
 */
public class ActionList<T>
{
    private List<Action<T>> Actions = new();
    private List<Action<T>> ToRemove = new();

    public void Add(Action<T> Action)
    {
        Actions.Add(Action);
    }

    public void Remove(Action<T> Action)
    {
        ToRemove.Add(Action);
    }

    public void ForEach(Action<Action<T>> Execution)
    {
        RemoveMarked();
        Actions.ForEach(_ => { if (_ != null) Execution(_); });
        RemoveMarked();
    }

    private void RemoveMarked()
    {
        foreach (var Action in ToRemove)
        {
            Actions.Remove(Action);
        }
        ToRemove.Clear();
    }


}
