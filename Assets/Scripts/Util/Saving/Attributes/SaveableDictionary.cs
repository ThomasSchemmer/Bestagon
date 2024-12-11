using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using static SaveableService;
using static SaveGameManager;

public class SaveableDictionary : SaveableData
{
    public override List<byte> Write(object DicValue, string Name)
    {
        return _Write(DicValue, Name);
    }

    public static new List<byte> _Write(object DicValue, string Name)
    {
        List<byte> InnerData = new();
        Type Type = DicValue.GetType();

        int i = 0;
        IDictionary Dictionary = DicValue as IDictionary;
        foreach (var Key in Dictionary.Keys)
        {
            List<byte> KeyData;
            if (TryGetKnownType(Key.GetType(), out var FoundVarType))
            {
                KeyData = WriteKnownType(Key, "" + i);
            }
            else
            {
                KeyData = Save(Key, "" + i);
            }

            object Value = Dictionary[Key];
            List<byte> ValueData;
            if (TryGetKnownType(Value.GetType(), out var _))
            {
                ValueData = WriteKnownType(Value, "" + i);
            }
            else
            {
                ValueData = Save(Value, "" + i);
            }

            InnerData.AddRange(KeyData);
            InnerData.AddRange(ValueData);
            i++;
        }

        List<byte> Data = WriteDicTypeHeader(DicValue, Name, InnerData.Count);
        Data.AddRange(InnerData);
        Data.AddRange(WriteTypeHeader(DicValue, Name, VariableType.DictionaryEnd));
        return Data;
    }

    private static List<byte> WriteDicTypeHeader(object Obj, string Name, int InnerLength)
    {
        if (Obj is not IDictionary Dic)
            return new();

        /*
         * Var:    Type | Hash | GenericNameLength1 | GenericName1 | GenericNameLength2 | GenericName2 | InnerLen    
         * #Byte:    1  | 4    | 4                  | 0..x         | 4                  | 0..x         | 4 
         */
        List<byte> Header = WriteTypeHeader(Obj, Name, VariableType.DictionaryStart);

        var GenArgs = Dic.GetType().GetGenericArguments();
        Type TypeA = GenArgs[0];
        Type TypeB = GenArgs[1];

        string TypeAName = TypeA.AssemblyQualifiedName;
        Header.AddRange(ToBytes(TypeAName.Length));
        Header.AddRange(ToBytes(TypeAName));
        string TypeBName = TypeB.AssemblyQualifiedName;
        Header.AddRange(ToBytes(TypeBName.Length));
        Header.AddRange(ToBytes(TypeBName));

        Header.AddRange(ToBytes(InnerLength));
        return Header;
    }

    public static int GetHeaderOffset(byte[] Data, int Index)
    {
        int TypeAOffset = GetStringVarOffset(Data, Index);
        Index += TypeAOffset;
        int TypeBOffset = GetStringVarOffset(Data, Index);
        Index += TypeBOffset;

        ReadInt(Data, Index, out int Length);
        return Length + sizeof(int) + TypeAOffset + TypeBOffset;
    }

    public static new object _ReadVar(byte[] Data, Tuple<VariableType, int, int> LoadedDict)
    {
        int Index = ReadDictTypeHeader(Data, LoadedDict.Item3 - GetBaseHeaderOffset(), out Type KeysType, out Type VarsType, out var InnerLength);

        var Types = new Type[2] { KeysType, VarsType };
        Type DictType = typeof(SerializedDictionary<,>);
        DictType = DictType.MakeGenericType(Types);
        IDictionary Dict = Activator.CreateInstance(DictType) as IDictionary;

        int Start = Index;
        int End = Index + InnerLength;
        IterateData(Data, Start, End, out var FoundVars);

        var Key = Activator.CreateInstance(KeysType);
        var Value = Activator.CreateInstance(VarsType);
        bool bIsKey = true;
        for (int i = 0; i < FoundVars.Count; i++)
        {
            if (IsEndType(FoundVars[i].Item1))
                continue;

            if (bIsKey)
            {
                Key = ReadVar(Data, FoundVars[i]);
            }
            else
            {
                Value = ReadVar(Data, FoundVars[i]);
                Dict.Add(Key, Value);
            }
            bIsKey = !bIsKey;
        }

        return Dict;
    }


    public static int ReadDictTypeHeader(byte[] Data, int Index, out Type KeysType, out Type VarsType, out int InnerLength)
    {
        Index = ReadByte(Data, Index, out byte bVarType);
        VariableType VarType = (VariableType)bVarType;
        if (VarType != VariableType.DictionaryStart)
        {
            throw new Exception("Expected a dictionary start, but found " + VarType + " instead!");
        }
        Index = ReadInt(Data, Index, out int _);
        Index = ReadString(Data, Index, out string KeysName);
        Index = ReadString(Data, Index, out string VarsName);
        Index = ReadInt(Data, Index, out InnerLength);

        KeysType = Type.GetType(KeysName);
        VarsType = Type.GetType(VarsName);
        return Index;
    }

    public static new FieldInfo _GetMatch(object Target, Tuple<VariableType, int, int> VarParams)
    {
        FieldInfo[] Fields = Target.GetType().GetFields(GetBindingFlags());
        foreach (var Field in Fields)
        {
            if (!Is(Field.FieldType))
                continue;

            if (Field.Name.GetHashCode() != VarParams.Item2)
                continue;

            return Field;
        }
        return null;
    }

    public static bool Is(Type Type)
    {
        return Type.IsGenericType &&
            (Type.GetGenericTypeDefinition() == typeof(Dictionary<,>) ||
            Type.GetGenericTypeDefinition() == typeof(SerializedDictionary<,>));
    }
}
