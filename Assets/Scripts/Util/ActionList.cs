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
        // some quest will add a follow-up quest to the list, making
        // iterating on the mainlist impossible. Easy fix: copy to a temp one
        List<Action<T>> Temp = new();
        Actions.ForEach(A => Temp.Add(A));
        Temp.ForEach(A => {
            if (A == null)
                return;

            if (A.Target == null || A.Method == null)
            {
                Remove(A);
                return;
            }

            Execution(A); 
        });
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

    public static bool IsValid(ActionList<T> ActionList)
    {
        if (ActionList == null)
            return false;

        return ActionList.Actions.Count >= 0;
    }
}

public class ActionList<X, Y>
{
    private List<Action<X, Y>> Actions = new();
    private List<Action<X, Y>> ToRemove = new();

    public void Add(Action<X, Y> Action)
    {
        Actions.Add(Action);
    }

    public void Remove(Action<X, Y> Action)
    {
        ToRemove.Add(Action);
    }

    public void ForEach(Action<Action<X, Y>> Execution)
    {
        RemoveMarked();
        List<Action<X, Y>> Temp = new();
        Actions.ForEach(A => Temp.Add(A));
        Temp.ForEach(A => {
            if (A == null)
                return;

            if (A.Target == null || A.Method == null)
            {
                Remove(A);
                return;
            }

            Execution(A);
        });
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

    public static bool IsValid(ActionList<X, Y> ActionList)
    {
        if (ActionList == null)
            return false;

        return ActionList.Actions.Count >= 0;
    }


}