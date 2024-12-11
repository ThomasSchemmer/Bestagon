using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/** 
 * Provides access to save data from services and cleanly load them even while the game
 * is already running
 */ 
public abstract class SaveableService : GameService
{
    // describes the usage of the saved data (stored in a wrapper)
    public enum SaveGameType
    {
        None = 0,
        MapGenerator = 1,
        CardGroups = 2,
        Stockpile = 5,
        Statistics = 6,
        Buildings = 7,
        Workers = 8,
        Units = 9,
        Malaise = 10,
        Spawning = 11,
        Quests = 12,
        Relics = 13,
        Decorations = 14
    }
    public enum VariableType : uint
    {
        None = 0,
        Boolean,
        Byte,
        Int,
        Uint,
        Float,
        Double,
        String,
        Vector3,
        Type,
        ClassStart,
        ClassEnd,
        ListStart,
        ListEnd,
        ArrayStart,
        ArrayEnd,
        WrapperStart,
        WrapperEnd,
        EnumStart,
        EnumEnd,
        DictionaryStart,
        DictionaryEnd,
    }

    public List<byte> Save(SaveGameType SGType, string Name)
    {
        List<byte> InnerData = SaveableData.Save(this, Name);
        List<byte> Data = WriteTypeHeader(this, Name, SGType, InnerData.Count);
        Data.AddRange(InnerData);
        Data.AddRange(WriteTypeHeader(this, Name, VariableType.WrapperEnd));

        return Data;
    }

    public void LoadFrom(byte[] Data, int MinIndex, int MaxIndex)
    {
        SaveableData.LoadTo(this, Data, MinIndex, MaxIndex);
    }

    private List<byte> WriteTypeHeader(object Obj, string Name, SaveGameType Type, int InnerLength)
    {
        /*
         * Var:    Type | Hash | SaveGameType | InnerLen    
         * #Byte:    1  | 4    | 1            | 4  
         */
        List<byte> Header = WriteTypeHeader(Obj, Name, VariableType.WrapperStart);
        Header.Add((byte)Type);
        Header.AddRange(SaveableData.ToBytes(InnerLength));
        return Header;
    }

    protected List<byte> WriteTypeHeader(object Obj, string Name, VariableType Type)
    {
        int Hash = Name.GetHashCode();
        List<byte> Bytes = new();
        Bytes.Add((byte)Type);
        Bytes.AddRange(SaveableData.ToBytes(Hash));
        return Bytes;
    }

    public static int ReadWrapperTypeHeader(byte[] Data, int Index, out int Hash, out SaveGameType Type, out int InnerLength)
    {
        Index = SaveableData.ReadByte(Data, Index, out byte bVarType);
        VariableType VarType = (VariableType)bVarType;
        if (VarType != VariableType.WrapperStart)
        {
            throw new Exception("Expected a wrapper start, but found " + VarType + " instead!");
        }
        Index = SaveableData.ReadInt(Data, Index, out Hash);
        Index = SaveableData.ReadByte(Data, Index, out byte bType);
        Index = SaveableData.ReadInt(Data, Index, out InnerLength);
        Type = (SaveGameType)bType;
        return Index;
    }

    /** Overwrite for actions that have to be taken after loading the data */
    public virtual void OnAfterLoaded() { }

    /** Overwrite for actions that have to be taken before loading the data */
    public virtual void OnBeforeLoaded() { }

    /** Overwrite for any actions that have to be taken right before saving */
    public virtual void OnBeforeSaved(bool bShouldReset) { }

    /** Overwrite for any actions that have to be taken right after saving */
    public virtual void OnAfterSaved() { }

}
