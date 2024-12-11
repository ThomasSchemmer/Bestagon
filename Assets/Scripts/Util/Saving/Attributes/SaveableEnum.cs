using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using static SaveableService;
using static SaveGameManager;

public class SaveableEnum : SaveableData
{
    public override List<byte> Write(object EnumValue, string Name)
    {
        return _Write(EnumValue, Name);
    }

    public static new List<byte> _Write(object EnumValue, string Name)
    {
        List<byte> InnerData = new();

        Type Type = EnumValue.GetType();

        List<byte> ItemData;
        FieldInfo Info = Type.GetFields(GetBindingFlags())[0];
        if (TryGetKnownType(Info.FieldType, out var _))
        {
            ItemData = WriteKnownType(Info.GetValue(EnumValue), Info.Name);
        }
        else
        {
            throw new Exception("Enum has to inherit from saveable base type!");
        }
        InnerData.AddRange(ItemData);

        List<byte> Data = WriteEnumTypeHeader(EnumValue, Name, InnerData.Count);
        Data.AddRange(InnerData);
        Data.AddRange(WriteTypeHeader(EnumValue, Name, VariableType.EnumEnd));
        return Data;
    }

    private static List<byte> WriteEnumTypeHeader(object Obj, string Name, int InnerLength)
    {
        /*
         * Var:    Type | Hash  | EnumNameLen  | EnumName | InnerLen    
         * #Byte:    1  | 4     | 4            | 0..X     | 4 
         */
        List<byte> Header = WriteTypeHeader(Obj, Name, VariableType.EnumStart);
        string AssemblyName = Obj.GetType().AssemblyQualifiedName;
        Header.AddRange(ToBytes(AssemblyName.Length));
        Header.AddRange(ToBytes(AssemblyName));
        Header.AddRange(ToBytes(InnerLength));
        return Header;
    }

    public static int GetHeaderOffset(byte[] Data, int Index)
    {
        int AssemblyNameOffset = GetStringVarOffset(Data, Index);
        ReadInt(Data, Index + AssemblyNameOffset, out int Length);
        return AssemblyNameOffset + Length + sizeof(int);
    }

    public static new object _ReadVar(byte[] Data, Tuple<VariableType, int, int> LoadedEnum)
    {
        int Index = ReadEnumTypeHeader(Data, LoadedEnum.Item3 - GetBaseHeaderOffset(), out int Hash, out Type EnumType, out int InnerLength);

        IterateData(Data, Index, Index + InnerLength, out var FoundVars);

        object EnumValue = ReadValue(Data, FoundVars[0]);
        return EnumValue;
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

    public static int ReadEnumTypeHeader(byte[] Data, int Index, out int Hash, out Type EnumType, out int InnerLength)
    {
        Index = ReadByte(Data, Index, out byte bVarType);
        VariableType VarType = (VariableType)bVarType;
        if (VarType != VariableType.EnumStart)
        {
            throw new Exception("Expected an enum start, but found " + VarType + " instead!");
        }
        Index = ReadInt(Data, Index, out Hash);
        Index = ReadString(Data, Index, out string AssemblyName);
        EnumType = Type.GetType(AssemblyName);
        Index = ReadInt(Data, Index, out InnerLength);
        return Index;
    }

    public static bool Is(Type Type)
    {
        return Type.IsEnum;
    }
}
