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

            Bytes[i] = (byte)Tuple.Key;
            // use slices to avoid looping
            NativeSlice<byte> Slice = new NativeSlice<byte>(Bytes, i + 1, Saveable.GetSize());
            Slice.CopyFrom(Saveable.GetData());

            i += Saveable.GetSize() + 1;
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

    public static int AddFloat(NativeArray<byte> Bytes, int Start, float Value)
    {
        NativeSlice<byte> Slice = new NativeSlice<byte>(Bytes, Start, sizeof(float));
        Slice.CopyFrom(BitConverter.GetBytes(Value));
        return Start + sizeof(float);
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
}

