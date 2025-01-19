using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using static SaveableService;
using static SaveGameManager;

public class SaveableArray : SaveableData
{
    public override List<byte> Write(object Array, string Name)
    {
        return _Write(Array, Name);
    }

    public static new List<byte> _Write(object Array, string Name)
    {
        List<byte> InnerData = new();

        Type ArrayType = Array.GetType();
        MethodInfo GetLengthMethod = ArrayType.GetMethod("get_Length");
        MethodInfo GetLengthsMethod = ArrayType.BaseType.GetMethod("GetLength");
        Type[] ParamTypes = new Type[2] { typeof(int), typeof(int) };
        MethodInfo GetItemMethod = ArrayType.GetMethod("GetValue", ParamTypes);

        int Rank = ArrayType.GetArrayRank();
        int[] Sizes = new int[Rank];
        int[] Indices = new int[Sizes.Length];
        for (int i = 0; i < Sizes.Length; i++)
        {
            Sizes[i] = (int)GetLengthsMethod.Invoke(Array, new object[] { i });
            Indices[i] = 0;
        }
        int ArrayCount = (int)GetLengthMethod.Invoke(Array, null);

        for (int i = 0; i < ArrayCount; i++)
        {
            object[] IndObjs = new object[Indices.Length];
            for (int j = 0; j < Indices.Length; j++)
            {
                IndObjs[j] = Indices[j];
            }
            object Item = GetItemMethod.Invoke(Array, IndObjs);
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

            IncreaseArrayIndices(Sizes, ref Indices);
        }

        List<byte> Data = WriteTypeHeader(Array, Name, Sizes, InnerData.Count);
        Data.AddRange(InnerData);
        Data.AddRange(WriteTypeHeader(Array, Name, VariableType.ArrayEnd));
        return Data;
    }

    private static void IncreaseArrayIndices(int[] Sizes, ref int[] Indices)
    {
        // array sizes are weirdly stored wrong order
        Indices[Indices.Length - 1]++;
        for (int j = Indices.Length - 1; j >= 0; j--)
        {
            if (Indices[j] < Sizes[j])
                continue;

            Indices[j] = 0;
            if (j - 1 >= 0)
            {
                Indices[j - 1]++;
            }
        }
    }

    private static List<byte> WriteTypeHeader(object Obj, string Name, int[] Dimensions, int InnerLength)
    {
        /*
         * Var:    Type | Hash  | GenericNameLength | GenericName | DimSize   | Dims  | InnerLen    
         * #Byte:    1  | 4     | 4                 | 0..x        | 1         | 0..X  | 4
         */
        List<byte> Header = WriteTypeHeader(Obj, Name, VariableType.ArrayStart);
        string AssemblyName = Obj.GetType().GetElementType().AssemblyQualifiedName;
        Header.AddRange(ToBytes(AssemblyName.Length));
        Header.AddRange(ToBytes(AssemblyName));
        Header.Add((byte)Dimensions.Length);
        for (int i = 0; i < Dimensions.Length; i++)
        {
            Header.Add((byte)Dimensions[i]);
        }
        Header.AddRange(ToBytes(InnerLength));
        return Header;
    }


    public static new object _ReadVar(byte[] Data, Tuple<VariableType, int, int> LoadedArray)
    {
        int Index = ReadArrayTypeHeader(Data, LoadedArray.Item3 - GetBaseHeaderOffset(), out Type ArrayType, out var Dimensions, out var InnerLength);
        var GenArray = Array.CreateInstance(ArrayType, Dimensions);

        int[] Indices = new int[Dimensions.Length];
        for (int i = 0; i < Dimensions.Length; i++)
        {
            Indices[i] = 0;
        }

        int StartIndex = Index;
        int EndIndex = Index + InnerLength;
        IterateData(Data, StartIndex, EndIndex, out var FoundArrayVars);

        for (int i = 0; i < FoundArrayVars.Count; i++)
        {
            if (IsEndType(FoundArrayVars[i].Item1))
                continue;

            object FoundArrayElement = ReadVar(Data, FoundArrayVars[i]);
            GenArray.SetValue(FoundArrayElement, Indices);
            IncreaseArrayIndices(Dimensions, ref Indices);
        }
        return GenArray;
    }

    public static int GetHeaderOffset(byte[] Data, int Index)
    {
        int AssemblyNameOffset = GetStringVarOffset(Data, Index);
        Index += AssemblyNameOffset;
        Index = ReadByte(Data, Index, out var DimSize);
        Index += DimSize * sizeof(byte);
        ReadInt(Data, Index, out int Length);
        return Length + sizeof(int) + DimSize * sizeof(byte) + sizeof(byte) + AssemblyNameOffset;
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

    public static int ReadArrayTypeHeader(byte[] Data, int Index, out Type ArrayType, out int[] Dimensions, out int InnerLength)
    {

        Index = ReadByte(Data, Index, out byte bVarType);
        VariableType VarType = (VariableType)bVarType;
        if (VarType != VariableType.ArrayStart)
        {
            throw new Exception("Expected a array start, but found " + VarType + " instead!");
        }
        Index = ReadInt(Data, Index, out int _);
        Index = ReadString(Data, Index, out string AssemblyName);
        Index = ReadByte(Data, Index, out byte bDimension);
        int Dimension = (int)bDimension;
        Dimensions = new int[Dimension];
        for (int i = 0; i < Dimension; i++)
        {
            Index = ReadByte(Data, Index, out byte bSize);
            Dimensions[i] = (int)bSize;
        }
        Index = ReadInt(Data, Index, out InnerLength);

        ArrayType = Type.GetType(AssemblyName);
        return Index;
    }

    public static bool Is(Type Type)
    {
        return Type.IsArray;
    }
}
