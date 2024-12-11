using System;
using System.Collections;
using System.Collections.Generic;
using static SaveGameManager;
using System.Reflection;
using UnityEngine;
using static SaveableService;

public class SaveableBaseType : SaveableData
{


    public override List<byte> Write(object Obj, string Name)
    {
        return _Write(Obj, Name);
    }

    public static new List<byte> _Write(object Obj, string Name)
    {
        Type Type = Obj.GetType();

        if (!Is(Type))
            return new();

        switch (TypeMap[Type])
        {
            case VariableType.Boolean: return WriteBoolean(Obj, Name);
            case VariableType.Byte: return WriteByte(Obj, Name);
            case VariableType.Int: return WriteInt(Obj, Name);
            case VariableType.Uint: return WriteUInt(Obj, Name);
            case VariableType.Float: return WriteFloat(Obj, Name);
            case VariableType.Double: return WriteDouble(Obj, Name);
            case VariableType.String: return WriteString(Obj, Name);
            case VariableType.Vector3: return WriteVector(Obj, Name);
            case VariableType.Type: return WriteType(Obj, Name);
            default:
                throw new Exception("Missing type registry for known type");
        }
    }

    public static new object _ReadVar(byte[] Data, Tuple<VariableType, int, int> FoundVar)
    {
        return ReadValue(Data, FoundVar);
    }

    public static new FieldInfo _GetMatch(object Target, Tuple<VariableType, int, int> VarParams)
    {
        FieldInfo[] Fields = Target.GetType().GetFields(GetBindingFlags());
        foreach (var Field in Fields)
        {
            Type FieldType = Field.FieldType;
            if (!TryGetKnownType(Field.FieldType, out var FoundType))
                continue;

            if (FoundType != VarParams.Item1)
                continue;

            if (Field.Name.GetHashCode() != VarParams.Item2)
                continue;

            return Field;
        }
        return null;
    }


    protected static List<byte> WriteInt(object Value, string Name)
    {
        List<byte> Bytes = WriteTypeHeader(Value, Name, VariableType.Int);

        int iValue = (int)Value;
        Bytes.AddRange(ToBytes(iValue));
        return Bytes;
    }

    protected static List<byte> WriteUInt(object Value, string Name)
    {
        List<byte> Bytes = WriteTypeHeader(Value, Name, VariableType.Uint);

        uint iValue = (uint)Value;
        Bytes.AddRange(ToBytes(iValue));
        return Bytes;
    }

    protected static List<byte> WriteByte(object Value, string Name)
    {
        List<byte> Bytes = WriteTypeHeader(Value, Name, VariableType.Byte);

        Bytes.Add((byte)Value);
        return Bytes;
    }

    protected static List<byte> WriteBoolean(object Value, string Name)
    {
        List<byte> Bytes = WriteTypeHeader(Value, Name, VariableType.Boolean);

        Bytes.Add(((bool)Value) ? (byte)1 : (byte)0);
        return Bytes;
    }

    protected static List<byte> WriteString(object Value, string Name)
    {
        List<byte> Bytes = WriteTypeHeader(Value, Name, VariableType.String);

        string Text = (string)Value;
        Bytes.AddRange(ToBytes(Text.Length));
        Bytes.AddRange(ToBytes(Text));
        return Bytes;
    }

    protected static List<byte> WriteVector(object Value, string Name)
    {
        List<byte> Bytes = WriteTypeHeader(Value, Name, VariableType.Vector3);

        Vector3 vValue = (Vector3)Value;
        Bytes.AddRange(ToBytes(vValue.x));
        Bytes.AddRange(ToBytes(vValue.y));
        Bytes.AddRange(ToBytes(vValue.z));
        return Bytes;
    }

    protected static List<byte> WriteType(object Value, string Name)
    {
        List<byte> Bytes = WriteTypeHeader(Value, Name, VariableType.Type);

        string AssemblyName = ((Type)Value).AssemblyQualifiedName;
        Bytes.AddRange(ToBytes(AssemblyName.Length));
        Bytes.AddRange(ToBytes(AssemblyName));
        return Bytes;
    }

    protected static List<byte> WriteDouble(object Value, string Name)
    {
        List<byte> Bytes = WriteTypeHeader(Value, Name, VariableType.Double);
        Bytes.AddRange(ToBytes((double)Value));
        return Bytes;
    }

    protected static List<byte> WriteFloat(object Value, string Name)
    {
        List<byte> Bytes = WriteTypeHeader(Value, Name, VariableType.Float);
        Bytes.AddRange(ToBytes((double)(float)Value));
        return Bytes;
    }

    public static bool Is(Type Type)
    {
        return TypeMap.ContainsKey(Type) || Type == typeof(System.Type);
    }
}
