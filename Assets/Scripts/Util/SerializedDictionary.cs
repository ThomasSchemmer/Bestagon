using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SerializedDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver where TKey : new()
{
    [Serializable]
    public class Tuple
    {
        public TKey Key;
        public TValue Value;
        public Tuple(TKey key, TValue value)
        {
            Key = key;
            Value = value;
        }
    }

    public List<Tuple> Tuples = new();

    public void OnAfterDeserialize()
    {
        Clear();
        for (int i = 0; i < Tuples.Count; i++)
        {
            TKey Key = Tuples[i].Key;
            if (!ContainsKey(Key))
            {
                Add(Key, Tuples[i].Value);
            }
            else 
            {
                TKey NewKey = new();
                if (!ContainsKey(NewKey))
                {
                    Add(NewKey, Tuples[i].Value);
                }
            }
        }
    }

    public TKey GetKeyAt(int i)
    {
        var Enumerator = Keys.GetEnumerator();
        for (int j = 0; j <= i; j++)
        {
            Enumerator.MoveNext();
        }
        return Enumerator.Current;
    }


    /** Unity duplicates the last entry on creating a new one, so on immediately serializing 
     * it tries to save the key twice - leading to only the last one being saved / displayed
     * Override this per (boilerplate) subclass to yield the next valid key 
     */

    public void OnBeforeSerialize()
    {
        if (Tuples != null)
        {
            Tuples.Clear();
        }
        else
        {
            Tuples = new List<Tuple>();
        }

        foreach (TKey Key in Keys)
        {
            TValue Value;
            if (!TryGetValue(Key, out Value))
                continue;

            Tuples.Add(new Tuple(Key, Value));
        }
    }
}

