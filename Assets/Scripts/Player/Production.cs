using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

[Serializable]
/**
 * Wrapper to have defined access to production data
 */ 
public class Production : ISaveable
{
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

    public static Production operator *(int A, Production B)
    {
        Production Production = new Production();
        foreach (Type Type in Enum.GetValues(typeof(Type)))
        {
            Production[Type] = A * B[Type];
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

    public int GetSize()
    {
        return GetStaticSize();
    }

    public static int GetStaticSize()
    {
        // each type has a enum index and amount of this resource 
        return Enum.GetValues(typeof(Type)).Length * 2;
    }

    public byte[] GetData()
    {
        NativeArray<byte> Bytes = new(GetSize(), Allocator.Temp);
        int Pos = 0;
        foreach (Type Type in Enum.GetValues(typeof(Type)))
        {
            Pos = SaveGameManager.AddEnumAsInt(Bytes, Pos, (int)Type);
            Pos = SaveGameManager.AddByte(Bytes, Pos, (byte)this[Type]);
        }

        return Bytes.ToArray();
    }

    public void SetData(NativeArray<byte> Bytes) {
        int Pos = 0;
        for (int i = 0; i < Enum.GetValues(typeof(Type)).Length; i++)
        {
            Pos = SaveGameManager.GetEnumAsInt(Bytes, Pos, out int iType);
            Pos = SaveGameManager.GetByte(Bytes, Pos, out byte bValue);
            Type Type = (Type)iType;
            this[Type] = (int)bValue;
        }
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
