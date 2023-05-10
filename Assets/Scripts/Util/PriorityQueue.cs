using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PriorityQueue<T> 
{
    public PriorityQueue() {
        _Elements = new();
    }

    public void Enqueue(T Item, int Priority) {
        int Index = _Elements.Count;
        for (int i = 0; i < _Elements.Count; i++) {
            Tuple<T, int> Element = _Elements[i];
            if (Priority < Element.Item2) {
                Index = i;
                break;
            }
        }

        _Elements.Insert(Index, new Tuple<T, int>(Item, Priority));
    }

    public Tuple<T, int> Dequeue() {
        if (_Elements.Count == 0)
            return null;

        Tuple<T, int> Result = _Elements[0];
        _Elements.RemoveAt(0);
        return Result;
    }

    public Tuple<T, int> this[int i] {
        get { return _Elements[i]; }
    }

    public int Count {
        get { return _Elements.Count; }
    }

    public List<Tuple<T, int>> _Elements;
}
