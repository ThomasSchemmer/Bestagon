using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SerializedDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
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
            if (ContainsKey(Tuples[i].Key))
            {
                this[Tuples[i].Key] = Tuples[i].Value;
            }
            else
            {
                Add(Tuples[i].Key, Tuples[i].Value);
            }
        }
    }

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
