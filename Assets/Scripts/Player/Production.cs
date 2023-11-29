using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
/**
 * Wrapper to have defined access to production data
 */ 
public class Production {
    [Serializable]
    public enum Type
    {
        Wood,
        Stone,
        Metal,
        Food,
        Science
    }

    public Production()
    {
        _Production = new SerializedDictionary<Type, int> ();
        foreach (Type Type in Enum.GetValues(typeof(Type)))
        {
            this[Type] = 0;
        }
    }

    public Production(Type Type, int Amount) : this() {
        this[Type] = Amount;
    }

    public Production(Type[] Types, int[] Amounts) : this()
    {
        if (Types.Length != Amounts.Length)
            return;

        for (int i = 0; i < Types.Length; i++)
        {
            this[Types[i]] = Amounts[i];
        }
    }

    public Production(Tuple<Type, int>[] Tuples) : this()
    {
        foreach (Tuple<Type, int> Tuple in Tuples)
        {
            this[Tuple.Key] = Tuple.Value;
        }
    }

    public static Production operator +(Production A, Production B) {
        Production Production = new Production();
        foreach (Type Type in Enum.GetValues(typeof(Type)))
        {
            Production[Type] = A[Type] + B[Type];
        }
        return Production;
    }

    public static Production operator -(Production A, Production B) {
        Production Production = new Production();
        foreach (Type Type in Enum.GetValues(typeof(Type)))
        {
            Production[Type] = A[Type] - B[Type];
        }
        return Production;
    }

    public static bool operator <=(Production A, Production B) {
        foreach (Type Type in Enum.GetValues(typeof(Type)))
        {
            if (!(A[Type] <= B[Type]))
                return false;
        }
        return true;
    }

    public static bool operator >=(Production A, Production B)
    {
        foreach (Type Type in Enum.GetValues(typeof(Type)))
        {
            if (!(A[Type] >= B[Type]))
                return false;
        }
        return true;
    }

    public static Production operator* (Production A, int B)
    {
        Production Production = new Production();
        foreach (Type Type in Enum.GetValues(typeof(Type)))
        {
            Production[Type] = A[Type] * B;
        }
        return Production;
    }

    public string GetDescription() {
        String String = "";
        foreach (Type Type in Enum.GetValues(typeof(Type)))
        {
            String += Type.ToString() + ": " + this[Type] + "\t";
        }
        return String;
    }

    public string GetDescription(Type Type) {
        return Type.ToString();
    }

    public string GetShortDescription() {
        string ProductionText = string.Empty;
        foreach (Type Type in Enum.GetValues(typeof(Type))) { 
            int Value = this[Type];
            if (Value == 0)
                continue;

            ProductionText += Value + GetShortDescription(Type) + " ";
        }

        return ProductionText;
    }

    public string GetShortDescription(Type Type) {
        return GetDescription(Type)[..1];
    }
  
    public SerializedDictionary<Type, int> _Production;
    public int this[Type Type]
    {
        get { return _Production.ContainsKey(Type) ? _Production[Type] : 0; }
        set { 
            if (_Production.ContainsKey(Type))
            {
                _Production[Type] = value;
            }
            else
            {
                _Production.Add(Type, value);
            } 
        }
    }

}
