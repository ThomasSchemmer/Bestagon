using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using static SaveableService;
using static SaveGameManager;

public class SaveableList : SaveableData
{
    public override List<byte> Write(object List, string Name)
    {
        return _Write(List, Name);
    }

    public static new List<byte> _Write(object List, string Name)
    {
        List<byte> InnerData = new();
        Type ListType = List.GetType();
        int ListCount = (int)ListType.GetProperty("Count").GetValue(List);
        MethodInfo GetItemMethod = ListType.GetMethod("get_Item");

        for (int i = 0; i < ListCount; i++)
        {
            object Item = GetItemMethod.Invoke(List, new object[] { i });
            List<byte> ItemData;
            if (TryGetKnownType(Item.GetType(), out var _))
            {
                ItemData = WriteKnownType(Item, "" + i);
            }
            else
            {
                ItemData = Save(Item, "" + i);
            }
            InnerData.AddRange(ItemData);
        }

        List<byte> Data = WriteTypeHeader(List, Name, InnerData.Count);
        Data.AddRange(InnerData);
        Data.AddRange(WriteTypeHeader(List, Name, VariableType.ListEnd));
        return Data;
    }

    private static List<byte> WriteTypeHeader(object Obj, string Name, int InnerLength)
    {
        if (Obj is not IList List)
            return new();

        /*
         * Var:    Type | Hash | GenericNameLength | GenericName | InnerLen    
         * #Byte:    1  | 4    | 4                 | 0..x        | 4 
         */
        List<byte> Header = WriteTypeHeader(Obj, Name, VariableType.ListStart);
        string AssemblyName = List.GetType().GetGenericArguments()[0].AssemblyQualifiedName;
        Header.AddRange(ToBytes(AssemblyName.Length));
        Header.AddRange(ToBytes(AssemblyName));
        Header.AddRange(ToBytes(InnerLength));
        return Header;
    }

    public static int GetHeaderOffset(byte[] Data, int Index)
    {
        int AssemblyNameOffset = GetStringVarOffset(Data, Index);
        Index += AssemblyNameOffset;
        ReadInt(Data, Index, out int Length);
        return AssemblyNameOffset + Length + sizeof(int);
    }

    public static new object _ReadVar(byte[] Data, Tuple<VariableType, int, int> LoadedList)
    {
        int Index = ReadListTypeHeader(Data, LoadedList.Item3 - GetBaseHeaderOffset(), out int Hash, out Type ListItemType, out int InnerLength);

        Type ListType = typeof(List<>);
        ListType = ListType.MakeGenericType(ListItemType);
        IList List = Activator.CreateInstance(ListType) as IList;

        int StartIndex = Index;
        int EndIndex = StartIndex + InnerLength;
        IterateData(Data, StartIndex, EndIndex, out var FoundListVars);

        for (int i = 0; i < FoundListVars.Count; i++)
        {
            if (IsEndType(FoundListVars[i].Item1))
                continue;

            object FoundListElement = ReadVar(Data, FoundListVars[i]);
            if (FoundListElement == null)
                continue;

            List.Add(FoundListElement);
        }
        return List;
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

    public static int ReadListTypeHeader(byte[] Data, int Index, out int Hash, out Type GenericType, out int InnerLength)
    {
        Index = ReadByte(Data, Index, out byte bVarType);
        VariableType VarType = (VariableType)bVarType;
        if (VarType != VariableType.ListStart)
        {
            throw new Exception("Expected a list start, but found " + VarType + " instead!");
        }
        Index = ReadInt(Data, Index, out Hash);
        Index = ReadString(Data, Index, out string AssemblyName);
        GenericType = Type.GetType(AssemblyName);
        Index = ReadInt(Data, Index, out InnerLength);
        return Index;
    }

    public static bool Is(Type Type)
    {
        return (Type.IsGenericType && (Type.GetGenericTypeDefinition() == typeof(List<>)));
    }
}
