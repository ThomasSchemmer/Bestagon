using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using static ISaveableService;

public class SaveGameManager : GameService
{
    public SerializedDictionary<SaveGameType, MonoBehaviour> Saveables;

    private SerializedDictionary<SaveGameType, int> FoundSaveableServices;

    public bool bLoadLastFile = false;
    // static so that it can be shared between scenes and a delayed load can be executed
    private static string FileToLoad = null;
    private static bool ShouldCreateNewFile = false;

    protected override void StartServiceInternal()
    {
        Game.RunAfterServicesInit((IconFactory IconFactory, MeshFactory MeshFactory) =>
        {
            Game.RunAfterServiceInit((CardFactory CardFactory) =>
            {
                HandleDelayedLoading();

                _OnInit?.Invoke(this);
            });
        });
    }

    protected override void StopServiceInternal(){ }

    private void HandleDelayedLoading()
    {
       if (bLoadLastFile && FileToLoad == null)
        {
            FileToLoad = GetMostRecentSave();
        }
        HandleSwitchToMenu();
        if (FileToLoad == null)
            return;

        Load();
    }

    private void HandleSwitchToMenu()
    {
        if (FileToLoad != null || ShouldCreateNewFile)
            return;

        if (SceneManager.GetActiveScene().name.Equals(Game.MenuSceneName))
            return;

        Game.LoadGame(null, Game.MenuSceneName, false);
    }

    public void OnSave()
    {
        Save();
    }

    public void OnLoad()
    {
        ShouldCreateNewFile = false;
        Load();
    }

    public void Load()
    {
        FindSaveables();
        ResetFoundServices();
        TryExecuteLoading();
    }

    public string Save()
    {
        int Size = GetSaveableSize();
        NativeArray<byte> Bytes = new(Size, Allocator.Temp);

        int i = 0;
        foreach (var Tuple in Saveables)
        {
            ISaveableData Saveable = Tuple.Value as ISaveableData;
            if (Saveable == null)
                continue;

            i = AddEnumAsByte(Bytes, i, (byte)Tuple.Key);
            i = AddInt(Bytes, i, Saveable.GetSize());
            i = AddSaveable(Bytes, i, Saveable);
        }

        string FileName = "Save.map";
        string FullPath = GetSavegamePath() + FileName;
        File.WriteAllBytes(FullPath, Bytes.ToArray());
        FileInfo fileInfo = new FileInfo(FullPath);
        fileInfo.IsReadOnly = false;
        return FileName;
    }

    public void MarkSaveForLoading(string FileName = null, bool bShouldCreateNewFile = false)
    {
        FileToLoad = FileName;
        ShouldCreateNewFile = bShouldCreateNewFile;
    }

    /** Requires the Manager to be already initialized, 
     * as only then the savegame mapping has been completed!
     * Enforcing it through callbacks would have been to bloated
     */    
    public bool HasDataFor(SaveGameType TargetType)
    {
        if (!IsInit)
        {
            throw new Exception("Cannot check for savegamedata with an unitialized manager - did you call RunAfterServiceInit?");
        }

        if (FoundSaveableServices == null)
            return false;

        return FoundSaveableServices.ContainsKey(TargetType);
    }

    public string[] GetSavegameNames()
    {
        string[] Files = Directory.GetFiles(GetSavegamePath());
        string[] Names = new string[Files.Length];
        for (int i = 0; i < Files.Length; i++)
        {
            Names[i] = Path.GetFileName(Files[i]);
        }
        return Names;
    }

    public string GetMostRecentSave()
    {
        string[] Saves = GetSavegameNames();
        DateTime MaxTime = DateTime.MinValue;
        int TargetIndex = -1;
        for (int i = 0; i < Saves.Length; i++) 
        {
            string Filename = GetSavegamePath() + Saves[i];
            FileInfo FileInfo = new FileInfo(Filename);
            if (FileInfo.CreationTime > MaxTime)
            {
                MaxTime = FileInfo.CreationTime;
                TargetIndex = i;
            }
        }
        return TargetIndex >= 0 ? Saves[TargetIndex] : "";
    }

    private void FindSaveables()
    {
        FoundSaveableServices = new();
        NativeArray<byte> Bytes = GetFileData();

        // no increase after loop, its done by reading the data
        for (int i = 0; i < Bytes.Length;)
        {
            i = GetEnumAsByte(Bytes, i, out byte Value);
            i = GetInt(Bytes, i, out int Size);
            SaveGameType Type = (SaveGameType)Value;
            if (!Saveables.TryGetValue(Type, out MonoBehaviour Behaviour))
            {
                i += Size;
                continue;
            }

            ISaveableService Saveable = Behaviour as ISaveableService;
            if (Saveable == null)
            {
                i += Size;
                continue;
            }

            FoundSaveableServices.Add(Type, i);
            i += Size;
        }
    }

    private NativeArray<byte> GetFileData()
    {
        string SaveGame = FileToLoad != null ? FileToLoad : GetMostRecentSave();
        string Filename = GetSavegamePath() + SaveGame;
        NativeArray<byte> Bytes = new(File.ReadAllBytes(Filename), Allocator.Temp);
        return Bytes;
    }

    private void ResetFoundServices()
    {
        foreach (var Tuple in FoundSaveableServices)
        {
            SaveGameType Type = Tuple.Key;
            if (!Saveables.TryGetValue(Type, out MonoBehaviour Behaviour))
                continue;

            ISaveableService Saveable = Behaviour as ISaveableService;
            if (Saveable == null)
                continue;

            Saveable.Reset();
        }
    }

    /** Loads the save and returns true if successful*/
    private bool TryExecuteLoading()
    {
        if (ShouldCreateNewFile)
            return false;

        if (FoundSaveableServices == null)
            return false;

        NativeArray<byte> Bytes = GetFileData();
        foreach (var Tuple in FoundSaveableServices)
        {
            SaveGameType Type = Tuple.Key;
            if (!Saveables.TryGetValue(Type, out MonoBehaviour Behaviour))
                continue;

            ISaveableService Saveable = Behaviour as ISaveableService;
            if (Saveable == null)
                continue;

            SetSaveable(Bytes, Tuple.Value, Saveable);

            Saveable.Load();
        }

        return true;
    }

    private int GetSaveableSize()
    {
        int Size = 0;
        foreach (var Tuple in Saveables)
        {
            ISaveableData Saveable = Tuple.Value as ISaveableData;
            if (Saveable == null)
                continue;

            // actual data + id field + size field
            // some of the saveables internally save their size as well,
            // but using/enforcing it would be too much overhead for the little saving space
            Size += Saveable.GetSize() + 1 + sizeof(int);
        }
        return Size;
    }

    /** Convenience function to create a new NativeArray and fill it with the base data */
    public static NativeArray<byte> GetArrayWithBaseFilled(ISaveableData Saveable, int BaseSize, byte[] BaseData)
    {
        NativeArray<byte> Bytes = new(Saveable.GetSize(), Allocator.Temp);
        NativeSlice<byte> Slice = new NativeSlice<byte>(Bytes, 0, BaseSize);
        Slice.CopyFrom(BaseData);
        return Bytes;
    }


    /** Convenience function to create a new NativeArray and fill it with the base data, but enfore a certain size of the base chunk */
    public static NativeArray<byte> GetArrayWithBaseFilled(int TotalSize, int BaseSize, byte[] BaseData)
    {
        NativeArray<byte> Bytes = new(TotalSize, Allocator.Temp);
        NativeSlice<byte> Slice = new NativeSlice<byte>(Bytes, 0, BaseSize);
        Slice.CopyFrom(BaseData);
        return Bytes;
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

    public static int AddEnumAsByte(NativeArray<byte> Bytes, int Start, byte Value)
    {
        NativeSlice<byte> Slice = new NativeSlice<byte>(Bytes, Start, 1);
        Slice[0] = Value;
        return Start + 1;
    }

    public static int AddBool(NativeArray<byte> Bytes, int Start, bool Value)
    {
        NativeSlice<byte> Slice = new NativeSlice<byte>(Bytes, Start, 1);
        Slice.CopyFrom(BitConverter.GetBytes(Value));
        return Start + 1;
    }

    public static int AddSaveable(NativeArray<byte> Bytes, int Start, ISaveableData Value)
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

    public static int GetEnumAsByte(NativeArray<byte> Bytes, int Start, out byte Value)
    {
        NativeSlice<byte> Slice = new NativeSlice<byte>(Bytes, Start, 1);
        Value = Slice[0];
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
    public static int SetSaveable(NativeArray<byte> Bytes, int Start, ISaveableData Saveable)
    {
        int Size = Saveable.ShouldLoadWithLoadedSize() ? LoadSizeOfSaveable(Bytes, Start, Saveable) : Saveable.GetSize();
        NativeSlice<byte> Slice = new NativeSlice<byte>(Bytes, Start, Size);
        NativeArray<byte> NewBytes = new(Slice.Length, Allocator.Temp);
        Slice.CopyTo(NewBytes);
        Saveable.SetData(NewBytes);
        return Start + Size;
    }

    private static int LoadSizeOfSaveable(NativeArray<byte> Bytes, int Start, ISaveableData Saveable)
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

    private static string GetSavegamePath()
    {
        string FilePath = Application.persistentDataPath + "/Maps/";
        if (!Directory.Exists(FilePath))
        {
            Directory.CreateDirectory(FilePath);
        }
        // can't use it as a field initializer
        return FilePath;
    }   
}

