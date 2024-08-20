using System;
using System.IO;
using Unity.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using static ISaveableService;

public class SaveGameManager : GameService
{
    public SerializedDictionary<SaveGameType, GameService> Saveables;

    private SerializedDictionary<SaveGameType, int> FoundSaveableServices;

    public bool bLoadLastFile = false;
    // static so that it can be shared between scenes and a delayed load can be executed
    private static string FileToLoad = null;
    // implicitly cancels loading savegame data
    private static bool bCreateNewFile = false;
    private static bool bLoadTutorial = false;

    private const string DefaultSaveGameName = "Save"+FileExtension;
    private const string FileExtension = ".map";
    private const string TutorialSavegame = "tutorial1";

    protected enum ServiceState
    {
        StartFresh,
        LoadFromSave,
        SwitchToMenu
    }

    protected override void StartServiceInternal()
    {
        Game.RunAfterServicesInit((IconFactory IconFactory, MeshFactory MeshFactory) =>
        {
            Game.RunAfterServiceInit((CardFactory CardFactory) =>
            {
                StartServices();
            });
        });
    }

    private void StartServices()
    {

        ServiceState State = HandleDelayedLoading();

        // the load menu scene has been triggered and will restart everything
        // swallow this logic run to prohibit services starting fresh and getting canceled
        if (State == ServiceState.SwitchToMenu)
            return;

        _OnInit?.Invoke(this);
    }

    protected override void StopServiceInternal(){ }

    private ServiceState HandleDelayedLoading()
    {
        if (bLoadTutorial && FileToLoad == null)
        {
            HandleTutorialLoading();
        }
        if (bLoadLastFile && FileToLoad == null)
        {
            FileToLoad = GetMostRecentSave();
        }

        if (TryHandleSwitchToMenu(out ServiceState State))
            return State;

        if (FileToLoad != null && FileToLoad.Equals(string.Empty))
        {
            FileToLoad = null;
        }
        // do not load savegame data and do not register as "having data" for service X
        // any service should simply start fresh
        if (FileToLoad == null)
            return ServiceState.StartFresh;

        LoadServices();
        return ServiceState.LoadFromSave;
    }

    /** Switches to the main menu when in any other scene in the editor*/
    private bool TryHandleSwitchToMenu(out ServiceState State)
    {
        State = default;
#if !UNITY_EDITOR
            return false;
#else
        if (FileToLoad != null || bCreateNewFile || bLoadTutorial)
            return false;

        if (SceneManager.GetActiveScene().name.Equals(Game.MenuSceneName))
            return false;

        Game.LoadGame(null, Game.MenuSceneName, false);
        State = ServiceState.SwitchToMenu;
        return true;
#endif
    }

    public void OnSave()
    {
        Save();
    }

    public void OnLoad()
    {
        bCreateNewFile = false;
        LoadServices();
    }

    public void LoadServices()
    {
        FindSaveables();
        ResetFoundServices();
        TryExecuteLoading();
    }

    public string Save(string SaveGameName = DefaultSaveGameName)
    {
        int Size = GetSaveableSize();
        NativeArray<byte> Bytes = new(Size, Allocator.Temp);

        int i = 0;
        foreach (var Tuple in Saveables)
        {
            ISaveableService SaveableService = Tuple.Value as ISaveableService;
            if (SaveableService == null)
                continue;
            if (!SaveableService.IsServiceInit())
                continue;

            i = AddEnumAsByte(Bytes, i, (byte)Tuple.Key);
            i = AddInt(Bytes, i, SaveableService.GetSize());
            i = AddSaveable(Bytes, i, SaveableService);
        }

        if (i != Bytes.Length)
        {
            throw new Exception("Error on saving: Did not fill savefile!");
        }

        string FileName = SaveGameName;
        string FullPath = GetSavegamePath() + FileName;
        File.WriteAllBytes(FullPath, Bytes.ToArray());
        FileInfo fileInfo = new FileInfo(FullPath);
        fileInfo.IsReadOnly = false;
        return FileName;
    }

    public void MarkSaveForLoading(string FileName = null, bool bShouldCreateNewFile = false, bool bShouldLoadTutorial = false)
    {
        FileToLoad = FileName;
        bCreateNewFile = bShouldCreateNewFile;
        bLoadTutorial = bShouldLoadTutorial;
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

    public static string[] GetSavegameNames()
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
        return TargetIndex >= 0 ? Saves[TargetIndex] : string.Empty;
    }

    public static string GetTutorialSave()
    {
        string[] Saves = GetSavegameNames();
        for (int i = 0; i < Saves.Length; i++)
        {
            if (!Saves[i].Equals(GetCompleteSaveGameName(TutorialSavegame)))
                continue;

            return Saves[i];
        }

        return string.Empty;
    }

    private void HandleTutorialLoading()
    {
        CopyTutorialFile();
        FileToLoad = GetTutorialSave();
        bCreateNewFile = false;

        if (Game.TryGetService(out TutorialSystem TutorialSystem))
        {
            TutorialSystem.SetInTutorial(!FileToLoad.Equals(string.Empty));
        }
    }

    private static bool HasTutorialFile()
    {
        return !GetTutorialSave().Equals(string.Empty);
    }

    private static void CopyTutorialFile()
    {
        MapContainer Container = Resources.Load("Maps/tutorial1") as MapContainer;

        string FileName = GetCompleteSaveGameName(TutorialSavegame);
        string Path = GetSavegamePath();
        string FilePath = Path + FileName;

        File.WriteAllBytes(FilePath, Container.MapData);
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
            if (!Saveables.TryGetValue(Type, out GameService Saveable))
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
        if (!IsInit)
            return;

        foreach (var Tuple in FoundSaveableServices)
        {
            SaveGameType Type = Tuple.Key;
            if (!Saveables.TryGetValue(Type, out GameService Service))
                continue;

            if (Service is not ISaveableService)
                continue;

            ISaveableService Saveable = Service as ISaveableService;
            Saveable.Reset();
        }
    }

    /** Loads the save and returns true if successful*/
    private bool TryExecuteLoading()
    {
        if (bCreateNewFile)
            return false;

        if (FoundSaveableServices == null)
            return false;

        NativeArray<byte> Bytes = GetFileData();
        foreach (var Tuple in FoundSaveableServices)
        {
            SaveGameType Type = Tuple.Key;
            if (!Saveables.TryGetValue(Type, out GameService Service))
                continue;

            if (Service is not ISaveableService)
                continue;

            ISaveableService Saveable = Service as ISaveableService;
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
            ISaveableService Saveable = Tuple.Value as ISaveableService;
            if (Saveable == null)
                continue;

            if (!Saveable.IsServiceInit())
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

    public static string GetClearName(string SaveGameName)
    {
        int Index = SaveGameName.IndexOf(FileExtension);
        if (Index == -1)
        {
            return SaveGameName;
        }
        return SaveGameName[..Index];
    }

    public static string GetCompleteSaveGameName(string SaveGameName)
    {
        int Index = SaveGameName.IndexOf(FileExtension);
        if (Index == -1)
            return SaveGameName + FileExtension;

        return SaveGameName;
    }

    public static string GetSavegamePath()
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

