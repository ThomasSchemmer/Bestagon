using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using static ISaveable;

public class SaveGameManager : GameService
{
    public SerializedDictionary<SaveGameType, MonoBehaviour> Saveables;

    private static string SaveGamePath = Application.dataPath + "/Resources/Maps/";


    protected override void StartServiceInternal()
    {
        /** hack to fully fill the saveables dictionary */
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

        _OnInit?.Invoke();
    }

    protected override void StopServiceInternal(){ }

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
            i = AddInt(Bytes, i, Saveable.GetSize());
            i = AddSaveable(Bytes, i, Saveable);
        }

        string Filename = SaveGamePath + "Save.map";
        System.IO.File.WriteAllBytes(Filename, Bytes.ToArray());
    }

    public void Load()
    {
        string Filename = SaveGamePath + "Save.map";
        NativeArray<byte> Bytes = new(System.IO.File.ReadAllBytes(Filename), Allocator.Temp);

        // no increase after loop, its done by reading the data
        for (int i = 0; i < Bytes.Length;)
        {
            i = GetEnumAsInt(Bytes, i, out int Value);
            i = GetInt(Bytes, i, out int Size);
            SaveGameType Type = (SaveGameType)Value;
            if (!Saveables.TryGetValue(Type, out MonoBehaviour Behaviour))
            {
                i += Size;
                continue;
            }

            ISaveable Saveable = Behaviour as ISaveable;
            if (Saveable == null)
            {
                i += Size;
                continue;
            }

            i = SetSaveable(Bytes, i, Saveable);

            Saveable.Load();
        }
    }

    private int GetSaveableSize()
    {
        int Size = 0;
        foreach (var Tuple in Saveables)
        {
            ISaveable Saveable = Tuple.Value as ISaveable;
            if (Saveable == null)
                continue;

            // actual data + id field + size field
            // some of the saveables internally save their size as well,
            // but using/enforcing it would be too much overhead for the little saving space
            Size += Saveable.GetSize() + 1 + sizeof(int);
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
        int Size = Value.Length;
        NativeSlice<byte> Slice = new NativeSlice<byte>(Bytes, Start, Size);
        byte[] Temp = System.Text.Encoding.UTF8.GetBytes(Value);
        Slice.CopyFrom(Temp);
        return Start + Size;
    }

    public static int AddRect(NativeArray<byte> Bytes, int Start, Rect Rect)
    {
        Start = AddDouble(Bytes, Start, Rect.x);
        Start = AddDouble(Bytes, Start, Rect.y);
        Start = AddDouble(Bytes, Start, Rect.width);
        Start = AddDouble(Bytes, Start, Rect.height);
        return Start;
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

    public static int GetBool(NativeArray<byte> Bytes, int Start, out bool Value)
    {
        NativeSlice<byte> Slice = new NativeSlice<byte>(Bytes, Start, sizeof(bool));
        Value = BitConverter.ToBoolean(Slice.ToArray());
        return Start + sizeof(bool);
    }

    /** Unlike the other getters we don't actually want to recreate the object, so we just set the values and keep the object
     * We might not be able to always load a saveable with the same size
     */
    public static int SetSaveable(NativeArray<byte> Bytes, int Start, ISaveable Saveable)
    {
        int Size = Saveable.ShouldLoadWithLoadedSize() ? LoadSizeOfSaveable(Bytes, Start, Saveable) : Saveable.GetSize();
        NativeSlice<byte> Slice = new NativeSlice<byte>(Bytes, Start, Size);
        NativeArray<byte> NewBytes = new(Slice.Length, Allocator.Temp);
        Slice.CopyTo(NewBytes);
        Saveable.SetData(NewBytes);
        return Start + Saveable.GetSize();
    }

    private static int LoadSizeOfSaveable(NativeArray<byte> Bytes, int Start, ISaveable Saveable)
    {
        // ignore reading of the size attribute, still start reading from the chunk origin
        GetInt(Bytes, Start, out int Size);

        return Size;
    }

    public static int GetString(NativeArray<byte> Bytes, int Start, int Length, out string Value)
    {
        NativeSlice<byte> Slice = new NativeSlice<byte>(Bytes, Start, Length);
        Value = System.Text.Encoding.UTF8.GetString(Slice.ToArray());
        return Start + Length;
    }

    public static int GetRect(NativeArray<byte> Bytes, int Start, out Rect Rect)
    {
        Start = GetDouble(Bytes, Start, out double X);
        Start = GetDouble(Bytes, Start, out double Y);
        Start = GetDouble(Bytes, Start, out double Width);
        Start = GetDouble(Bytes, Start, out double Height);
        Rect = new Rect((float)X, (float)Y, (float)Width, (float)Height);

        return Start;
    }
}

