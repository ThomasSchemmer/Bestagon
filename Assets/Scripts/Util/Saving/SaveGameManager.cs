using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public class SaveGameManager : MonoBehaviour
{
    public SerializedDictionary<SaveGameType, MonoBehaviour> Saveables;

    private static string SaveGamePath = Application.dataPath + "/Resources/Maps/";

    public enum SaveGameType
    {
        MapGenerator = 0
    }

    /** hack to fully fill the saveables dictionary */
    public void Start()
    {
        if (Saveables == null)
        {
            Saveables = new();
        }

        foreach (var Type in Enum.GetValues(typeof(SaveGameType)))
        {
            if (Saveables.ContainsKey((SaveGameType)Type))
                continue;

            Saveables.Add((SaveGameType)Type, null);
        }
    }

    public void Save()
    {
        int Size = GetSaveableSize();
        NativeArray<byte> Bytes = new(Size, Allocator.Temp);

        int i = 0;
        foreach (var Tuple in Saveables)
        {
            ISaveable Saveable = Tuple.Value as ISaveable;
            if (Saveable == null)
                continue;

            i = AddEnumAsInt(Bytes, i, (int)Tuple.Key);
            i = AddSaveable(Bytes, i, Saveable);
        }

        string Filename = SaveGamePath + "Save.map";
        System.IO.File.WriteAllBytes(Filename, Bytes.ToArray());
    }

    public void Load()
    {
        string Filename = SaveGamePath + "Save.map";
        NativeArray<byte> Bytes = new(System.IO.File.ReadAllBytes(Filename), Allocator.Temp);

        for (int i = 0; i < Bytes.Length; i++)
        {
            SaveGameType Type = (SaveGameType)Bytes[i];
            if (!Saveables.TryGetValue(Type, out MonoBehaviour Behaviour))
                return;

            ISaveable Saveable = Behaviour as ISaveable;
            if (Saveable == null)
                return;

            NativeSlice<byte> Slice = new NativeSlice<byte>(Bytes, i + 1, Saveable.GetSize());
            Saveable.SetData(Slice.ToArray());

            i += Saveable.GetSize() + 1;
        }

        if (!Game.TryGetService(out MapGenerator Generator))
            return;

        Generator.GenerateMap();

    }

    private int GetSaveableSize()
    {
        int Size = 0;
        foreach (var Tuple in Saveables)
        {
            ISaveable Saveable = Tuple.Value as ISaveable;
            if (Saveable == null)
                continue;

            // actual data + id field
            Size += Saveable.GetSize() + 1;
        }
        return Size;
    }

    public static int AddInt(NativeArray<byte> Bytes, int Start, int Value)
    {
        NativeSlice<byte> Slice = new NativeSlice<byte>(Bytes, Start, sizeof(int));
        Slice.CopyFrom(BitConverter.GetBytes(Value));
        return Start + sizeof(int);
    }

    public static int AddByte(NativeArray<byte> Bytes, int Start, byte Value)
    {
        NativeSlice<byte> Slice = new NativeSlice<byte>(Bytes, Start, 1);
        Slice[0] = Value;
        return Start + 1;
    }

    public static int AddDouble(NativeArray<byte> Bytes, int Start, double Value)
    {
        NativeSlice<byte> Slice = new NativeSlice<byte>(Bytes, Start, sizeof(double));
        Slice.CopyFrom(BitConverter.GetBytes(Value));
        return Start + sizeof(double);
    }

    public static int AddEnumAsInt(NativeArray<byte> Bytes, int Start, int Value)
    {
        NativeSlice<byte> Slice = new NativeSlice<byte>(Bytes, Start, 1);
        Slice[0] = (byte)Value;
        return Start + 1;
    }

    public static int AddBool(NativeArray<byte> Bytes, int Start, bool Value)
    {
        NativeSlice<byte> Slice = new NativeSlice<byte>(Bytes, Start, 1);
        Slice.CopyFrom(BitConverter.GetBytes(Value));
        return Start + 1;
    }

    public static int AddSaveable(NativeArray<byte> Bytes, int Start, ISaveable Value)
    {
        NativeSlice<byte> Slice = new NativeSlice<byte>(Bytes, Start, Value.GetSize());
        Slice.CopyFrom(Value.GetData());
        return Start + Value.GetSize();
    }

    public static int AddString(NativeArray<byte> Bytes, int Start, string Value)
    {
        int Size = Value.Length * sizeof(char);
        NativeSlice<byte> Slice = new NativeSlice<byte>(Bytes, Start, Size);
        Slice.CopyFrom(System.Text.Encoding.UTF8.GetBytes(Value));
        return Start + Size;
    }

    public static int GetInt(NativeArray<byte> Bytes, int Start, out int Value)
    {
        NativeSlice<byte> Slice = new NativeSlice<byte>(Bytes, Start, sizeof(int));
        Value = BitConverter.ToInt32(Slice.ToArray());
        return Start + sizeof(int);
    }

    public static int GetByte(NativeArray<byte> Bytes, int Start, out byte Value)
    {
        NativeSlice<byte> Slice = new NativeSlice<byte>(Bytes, Start, 1);
        Value = Slice[0];
        return Start + 1;
    }

    public static int GetDouble(NativeArray<byte> Bytes, int Start, out double Value)
    {
        NativeSlice<byte> Slice = new NativeSlice<byte>(Bytes, Start, sizeof(double));
        Value = BitConverter.ToDouble(Slice.ToArray());
        return Start + sizeof(double);
    }

    public static int GetEnumAsInt(NativeArray<byte> Bytes, int Start, out int Value)
    {
        NativeSlice<byte> Slice = new NativeSlice<byte>(Bytes, Start, 1);
        Value = (int)Slice[0];
        return Start + 1;
    }

    public static int GetBool(NativeArray<byte> Bytes, int Start, bool Value)
    {
        NativeSlice<byte> Slice = new NativeSlice<byte>(Bytes, Start, sizeof(bool));
        Value = BitConverter.ToBoolean(Slice.ToArray());
        return Start + sizeof(bool);
    }

    /** Unlike the other getters we don't actually want to recreate the object, so we just set the values and keep the object*/
    public static int SetSaveable(NativeArray<byte> Bytes, int Start, ISaveable Value)
    {
        NativeSlice<byte> Slice = new NativeSlice<byte>(Bytes, Start, Value.GetSize());
        Value.SetData(Slice.ToArray());
        return Start + Value.GetSize();
    }

    public static int GetString(NativeArray<byte> Bytes, int Start, int Length, out string Value)
    {
        int Size = Length * sizeof(char);
        NativeSlice<byte> Slice = new NativeSlice<byte>(Bytes, Start, Size);
        Value = System.Text.Encoding.UTF8.GetString(Slice.ToArray());
        return Start + Size;
    }
}

