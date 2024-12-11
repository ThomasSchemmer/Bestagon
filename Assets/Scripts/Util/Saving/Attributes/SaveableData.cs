using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using static SaveableService;
using static SaveGameManager;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public abstract class SaveableData : System.Attribute {

    protected static Dictionary<Type, VariableType> TypeMap = new()
    {
        { typeof(bool), VariableType.Boolean },
        { typeof(byte), VariableType.Byte },
        { typeof(int), VariableType.Int },
        { typeof(uint), VariableType.Uint },
        { typeof(float), VariableType.Float },
        { typeof(double), VariableType.Double },
        { typeof(string), VariableType.String },
        { typeof(Vector3), VariableType.Vector3 },
        // cant use System.RuntimeType directly
        { typeof(Type).GetType(), VariableType.Type },
        { typeof(Type), VariableType.Type },
    }; 
    
    // todo: prettify
    protected static Dictionary<VariableType, Type> ClassMap = new()
    {
        {VariableType.Boolean, typeof(SaveableBaseType)},
        {VariableType.Byte, typeof(SaveableBaseType)},
        {VariableType.Int, typeof(SaveableBaseType)},
        {VariableType.Uint, typeof(SaveableBaseType)},
        {VariableType.Float, typeof(SaveableBaseType)},
        {VariableType.Double, typeof(SaveableBaseType)},
        {VariableType.String, typeof(SaveableBaseType)},
        {VariableType.Vector3, typeof(SaveableBaseType)},
        {VariableType.Type, typeof(SaveableBaseType)},
        {VariableType.ClassStart, typeof(SaveableClass)},
        {VariableType.ClassEnd, null},
        {VariableType.ListStart, typeof(SaveableList)},
        {VariableType.ListEnd, null},
        {VariableType.ArrayStart, typeof(SaveableArray)},
        {VariableType.ArrayEnd, null},
        // wrapper are handled indirectly in SaveableService
        {VariableType.WrapperStart, null},
        {VariableType.WrapperEnd, null},
        {VariableType.EnumStart, typeof(SaveableEnum)},
        {VariableType.EnumEnd, null},
        {VariableType.DictionaryStart, typeof(SaveableDictionary)},
        {VariableType.DictionaryEnd, null},
    };

    // runtime only!
    protected static Dictionary<VariableType, MethodInfo> _ReadVarMap = new();

    //**************************** Saving **************************************************************

    public abstract List<byte> Write(object Obj, string Name);

    public static List<byte> Save(object Obj, string Name)
    {
        FieldInfo[] Infos = Obj.GetType().GetFields(GetBindingFlags());

        List<byte> InnerData = new();

        bool bHasSaved = false;
        foreach (FieldInfo Info in Infos)
        {
            if (!TryGetSaveable(Info, null, out var FoundSaveable))
                continue;

            // even though we technically might not have saved null things, we at least found something saveable
            bHasSaved = true;
            object Value = Info.GetValue(Obj);
            if (Value == null)
                continue;

            // use non-static call to get the correct method overwrite
            InnerData.AddRange(FoundSaveable.Write(Info.GetValue(Obj), Info.Name));
        }

        if (!bHasSaved)
        {
            throw new Exception("Nothing saved for " + Obj.ToString() + " - are you missing a type mapping?");
        }

        List<byte> Data = SaveableClass.WriteHeader(Obj, Name, InnerData.Count);
        Data.AddRange(InnerData);
        Data.AddRange(WriteTypeHeader(Obj, Name, VariableType.ClassEnd));

        return Data;
    }

    public static List<byte> WriteKnownType(object Value, string Name)
    {
        Type Type = Value.GetType();
        if (!TryGetKnownType(Type, out var FoundVarType))
            return new();

        if (SaveableBaseType.Is(Type))
        {
            return SaveableBaseType._Write(Value, Name);
        }

        //c# has problems detecting subclasses in generic types
        switch (FoundVarType)
        {
            case VariableType.EnumStart: return SaveableEnum._Write(Value, Name);
            case VariableType.ListStart: return SaveableList._Write(Value, Name);
            case VariableType.ArrayStart: return SaveableArray._Write(Value, Name);
            case VariableType.DictionaryStart: return SaveableDictionary._Write(Value, Name);
            default:
                throw new Exception("Missing type registry for known type");
        }
    }

    //**************************** Loading **************************************************************

    protected static object ReadVar(byte[] Data, Tuple<VariableType, int, int> FoundVar)
    {
        VariableType VarType = FoundVar.Item1;
        if (!_ReadVarMap.ContainsKey(VarType))
        {
            Type FoundClassType = ClassMap[VarType];
            if (FoundClassType == null)
                return null;

            // use reflection to get the static overload
            MethodInfo Method = FoundClassType.GetMethod("_ReadVar");
            if (Method == null)
                throw new NotImplementedException("Every SaveableData class needs to overwrite this!");

            _ReadVarMap.Add(VarType, Method);
        }

        MethodInfo ReadVarMethod = _ReadVarMap[VarType];
        object Variable = ReadVarMethod.Invoke(null, new object[] { Data, FoundVar });
        return Variable;
    }


    public static FieldInfo GetMatch(object Target, Tuple<VariableType, int, int> FoundVar)
    {
        Type FoundClassType = ClassMap[FoundVar.Item1];
        if (FoundClassType == null)
            return null;

        // use reflection to get the static overload
        // (has to be static since we dont have the actual field and its restrictions yet)
        MethodInfo GetMatchMethod = FoundClassType.GetMethod("_GetMatch");
        if (GetMatchMethod == null)
            throw new NotImplementedException("Every SaveableData class needs to overwrite this!");

        FieldInfo FoundField = GetMatchMethod.Invoke(Target, new object[] { Target, FoundVar }) as FieldInfo;
        return FoundField;
    }

    /** Returns the field matching the variable meta data */
    public static FieldInfo _GetMatch(object Target, Tuple<VariableType, int, int> VarParams)
    {
        // has to be overwritten in subclasses
        throw new NotImplementedException();
    }

    /** Tries to load the passed in data into the target's best fitting variable */
    public static void LoadTo(object Target, byte[] Data, int MinIndex, int MaxIndex)
    {
        int Index = SaveableClass.ReadHeader(Data, MinIndex, out int BeginHash, out Type ClassType, out int _);
        if (Target.GetType() != ClassType)
            return;

        IterateData(Data, Index, MaxIndex, out var FoundVars);

        foreach (var FoundVar in FoundVars)
        {
            Type FoundClassType = ClassMap[FoundVar.Item1];
            if (FoundClassType == null)
                continue;

            FieldInfo FoundField = GetMatch(Target, FoundVar);
            // eg the new version doesn't have the var anymore
            if (FoundField == null)
                continue;

            object LoadedObject = ReadVar(Data, FoundVar);
            FoundField.SetValue(Target, LoadedObject);
        }
    }

    public static void IterateData(byte[] Data, int Index, int MaxIndex, out List<Tuple<VariableType, int, int>> FoundVars)
    {
        FoundVars = new();
        while (Index < MaxIndex)
        {
            // shallow search only, list/classes will be filled later!
            Index = ReadSaveTypeHeader(Data, Index, out var VarTuple);
            FoundVars.Add(VarTuple);
        }
    }

    private static int ReadSaveTypeHeader(byte[] Data, int Index, out Tuple<VariableType, int, int> FoundVar)
    {
        Index = ReadByte(Data, Index, out byte bVarType);
        VariableType Type = (VariableType)bVarType;
        Index = ReadInt(Data, Index, out int Hash);
        FoundVar = new(Type, Hash, Index);

        int VarOffset;
        switch (Type)
        {
            case VariableType.Boolean: VarOffset = GetByteVarOffset(); break;
            case VariableType.Byte: VarOffset = GetByteVarOffset(); break;
            case VariableType.Int: VarOffset = GetIntVarOffset(); break;
            case VariableType.Uint: VarOffset = GetUIntVarOffset(); break;
            case VariableType.Float: // fallthrough
            case VariableType.Double: VarOffset = GetDoubleVarOffset(); break;
            case VariableType.String: VarOffset = GetStringVarOffset(Data, Index); break;
            case VariableType.Vector3: VarOffset = GetVectorVarOffset(); break;
            case VariableType.Type: VarOffset = GetTypeVarOffset(Data, Index); break;
            case VariableType.ClassStart: VarOffset = SaveableClass.GetHeaderOffset(Data, Index); break;
            case VariableType.ListStart: VarOffset = SaveableList.GetHeaderOffset(Data, Index); break;
            case VariableType.ArrayStart: VarOffset = SaveableArray.GetHeaderOffset(Data, Index); break;
            case VariableType.WrapperStart: VarOffset = GetWrapperHeaderOffset(Data, Index); break;
            case VariableType.EnumStart: VarOffset = SaveableEnum.GetHeaderOffset(Data, Index); break;
            case VariableType.DictionaryStart: VarOffset = SaveableDictionary.GetHeaderOffset(Data, Index); break;
            case VariableType.ListEnd: VarOffset = 0; break;
            case VariableType.ClassEnd: VarOffset = 0; break;
            case VariableType.ArrayEnd: VarOffset = 0; break;
            case VariableType.WrapperEnd: VarOffset = 0; break;
            case VariableType.EnumEnd: VarOffset = 0; break;
            case VariableType.DictionaryEnd: VarOffset = 0; break;
            default:
                throw new Exception("Should not reach here - are you missing a value type?");
        }
        return Index + VarOffset;
    }

    //**************************** static overrides **************************************************************


    /** Reads the variable provided with the meta data according to its subclasses definition */
    public static object _ReadVar(byte[] Data, Tuple<VariableType, int, int> FoundVar)
    {
        // has to be overwritten in subclasses
        throw new NotImplementedException();
    }
    public static List<byte> _Write(object Obj, string Name)
    {
        // has to be overwritten in subclasses
        throw new NotImplementedException();
    }


    //**************************** Utility **************************************************************

    protected static bool TryGetKnownType(Type Type, out VariableType VariableType)
    {
        if (SaveableBaseType.Is(Type))
        {
            VariableType = TypeMap[Type];
            return true;
        }
        // c# is weird for saving enums, converts it into a basetype?
        if (SaveableEnum.Is(Type))
        {
            VariableType = VariableType.EnumStart;
            return true;
        }
        if (SaveableArray.Is(Type))
        {
            VariableType = VariableType.ArrayStart;
            return true;
        }
        if (SaveableList.Is(Type))
        {
            VariableType = VariableType.ListStart;
            return true;
        }
        if (SaveableDictionary.Is(Type))
        {
            VariableType = VariableType.DictionaryStart;
            return true;
        }
        VariableType = default;
        return false;
    }

    protected static object ReadValue(byte[] Data, Tuple<VariableType, int, int> Variable)
    {
        return ReadValue(Data, Variable.Item3, Variable.Item1);
    }

    protected static object ReadValue(byte[] Data, int Index, Type Type)
    {
        if (TryGetKnownType(Type, out var FoundType))
            return null;

        return ReadValue(Data, Index, FoundType);
    }

    protected static object ReadValue(byte[] Data, int Index, VariableType Type)
    {
        switch (Type)
        {
            case VariableType.Boolean: ReadBoolean(Data, Index, out bool boValue); return boValue;
            case VariableType.Byte: ReadByte(Data, Index, out byte bValue); return bValue;
            case VariableType.Int: ReadInt(Data, Index, out int iValue); return iValue;
            case VariableType.Uint: ReadUInt(Data, Index, out uint uValue); return uValue;
            case VariableType.Float: ReadFloat(Data, Index, out float fValue); return fValue;
            case VariableType.Double: ReadDouble(Data, Index, out double dValue); return dValue;
            case VariableType.String: ReadString(Data, Index, out string sValue); return sValue;
            case VariableType.Vector3: ReadVector(Data, Index, out Vector3 vValue); return vValue;
            case VariableType.Type: ReadType(Data, Index, out Type tValue); return tValue;
        }
        return null;
    }

    public static int ReadBoolean(byte[] Data, int Index, out bool Value)
    {
        Value = Data[Index] > 0;
        return Index + sizeof(byte);
    }

    public static int ReadByte(byte[] Data, int Index, out byte Value)
    {
        Value = Data[Index];
        return Index + sizeof(byte);
    }

    public static int ReadInt(byte[] Data, int Index, out int Value)
    {
        Value = BitConverter.ToInt32(Data, Index);
        return Index + sizeof(int);
    }

    public static int ReadUInt(byte[] Data, int Index, out uint Value)
    {
        Value = BitConverter.ToUInt32(Data, Index);
        return Index + sizeof(uint);
    }

    public static int ReadDouble(byte[] Data, int Index, out double Value)
    {
        Value = BitConverter.ToDouble(Data, Index);
        return Index + sizeof(double);
    }
    public static int ReadFloat(byte[] Data, int Index, out float Value)
    {
        double dValue = BitConverter.ToDouble(Data, Index);
        Value = (float)dValue;
        return Index + sizeof(double);
    }

    public static int ReadString(byte[] Data, int Index, out string Value)
    {
        Index = ReadInt(Data, Index, out int Length);
        Value = Encoding.UTF8.GetString(Data, Index, Length);
        return Index + Length;
    }

    public static int ReadVector(byte[] Data, int Index, out Vector3 Value)
    {
        Index = ReadDouble(Data, Index, out double x);
        Index = ReadDouble(Data, Index, out double y);
        Index = ReadDouble(Data, Index, out double z);
        Value = new((float)x, (float)y, (float)z);
        // todo: might be wrong?
        return Index + sizeof(double) * 3;
    }

    public static int ReadType(byte[] Data, int Index, out Type Type)
    {
        Index = ReadString(Data, Index, out string TypeName);
        Type = Type.GetType(TypeName);
        return Index; 
    }

    protected static int GetIntVarOffset()
    {
        return sizeof(int);
    }

    protected static int GetUIntVarOffset()
    {
        return sizeof(uint);
    }

    protected static int GetByteVarOffset()
    {
        return sizeof(byte);
    }

    protected static int GetBooleanVarOffset()
    {
        return sizeof(byte);
    }

    protected static int GetDoubleVarOffset()
    {
        return sizeof(double);
    }

    protected static int GetStringVarOffset(byte[] Data, int Index)
    {
        ReadInt(Data, Index, out int Length);
        return sizeof(int) + Length;
    }

    protected static int GetTypeVarOffset(byte[] Data, int Index)
    {
        ReadString(Data, Index, out string Type);
        return sizeof(int) + Type.Length;
    }

    protected static int GetVectorVarOffset()
    {
        return sizeof(double) * 3;
    }


    public static int GetBaseHeaderOffset()
    {
        return sizeof(int) + sizeof(byte);
    }

    protected static int GetWrapperHeaderOffset(byte[] Data, int Index)
    {
        Index = ReadByte(Data, Index, out var _);
        ReadInt(Data, Index, out int Length);
        return Length + sizeof(int) + sizeof(byte);
    }

    protected static bool IsEndType(VariableType Type)
    {
        return Type == VariableType.WrapperEnd || Type == VariableType.ClassEnd || Type == VariableType.ListEnd ||
            Type == VariableType.ArrayEnd || Type == VariableType.EnumEnd || Type == VariableType.DictionaryEnd;
    }

    public static List<byte> ToBytes(int Value)
    {
        return BitConverter.GetBytes(Value).ToList();
    }

    public static List<byte> ToBytes(uint Value)
    {
        return BitConverter.GetBytes(Value).ToList();
    }

    public static List<byte> ToBytes(double Value)
    {
        return BitConverter.GetBytes(Value).ToList();
    }

    public static List<byte> ToBytes(string Value)
    {
        return Encoding.UTF8.GetBytes(Value).ToList();
    }

    protected static List<byte> WriteTypeHeader(object Obj, string Name, VariableType Type)
    {
        int Hash = Name.GetHashCode();
        List<byte> Bytes = new();
        Bytes.Add((byte)Type);
        Bytes.AddRange(ToBytes(Hash));
        return Bytes;
    }

    protected static bool TryGetSaveable(FieldInfo Info, Type TargetType, out SaveableData FoundSaveable)
    {
        object[] attrs = Info.GetCustomAttributes(true);
        foreach (object attr in attrs)
        {
            SaveableData SaveableAttr = attr as SaveableData;
            if (SaveableAttr == null)
                continue;

            if (TargetType != null && TargetType != SaveableAttr.GetType())
                continue;

            FoundSaveable = SaveableAttr;
            return true;
        }

        FoundSaveable = default;
        return false;
    }

    protected static bool TryGetSaveable(FieldInfo[] Infos, Type SaveableType, out SaveableData FoundSaveable)
    {
        foreach (var Info in Infos)
        {
            if (!TryGetSaveable(Info, SaveableType, out FoundSaveable))
                continue;

            return true;
        }

        FoundSaveable = default;
        return false;
    }

    protected static BindingFlags GetBindingFlags()
    {
        return BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public;
    }

}
