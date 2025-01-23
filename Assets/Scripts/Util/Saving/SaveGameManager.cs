using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Unity.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using static SaveableService;

/**
 * Manages all savegame related things
 * Used to switch into the main menu from any game scene inside the unity editor
 * Also provides utility functions to map data into byte structures
 * 
 * Keeps track of @Saveables in order to map loadable data 
 * 
 * Loading flow:
 * - Find all saveables in data that match the scene saveables
 *   (any non-found, but listed in @Game will be started normally)
 * - Fill saveables with found data
 * - call @OnLoaded for Saveable, initializing it
 */
public class SaveGameManager : GameService
{

    public SerializedDictionary<SaveGameType, GameService> Saveables;

    // stores location in current file
    private Dictionary<SaveGameType, int> FoundSaveableServices;

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
        ResetAllServices();
        Game.RunAfterServicesInit((IconFactory IconFactory, MeshFactory MeshFactory) =>
        {
            Game.RunAfterServicesInit((CardFactory CardFactory, GameplayAbilitySystem GAS) =>
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

    public void OnSave()
    {
        Save(false);
    }

    public void OnLoad()
    {
        bCreateNewFile = false;
        LoadServices();
    }

    public void LoadServices()
    {
        ResetAllServices();
        FindSaveables();
        TryExecuteLoading();
    }

    public string Save(bool bShouldReset, string SaveGameName = DefaultSaveGameName)
    {
        foreach (var Tuple in Saveables)
        {
            SaveableService SaveableService = Tuple.Value as SaveableService;
            if (SaveableService == null)
                continue;
            if (!SaveableService.IsInit)
                continue;

            SaveableService.OnBeforeSaved(bShouldReset);
        }

        List<byte> Data = new List<byte>();
        foreach (var Tuple in Saveables)
        {
            SaveableService SaveableService = Tuple.Value as SaveableService;
            if (SaveableService == null)
                continue;
            if (!SaveableService.IsInit)
                continue;

            Data.AddRange(SaveableService.Save(Tuple.Key, SaveableService.name));
            if (!bShouldReset)
                continue;

            SaveableService.Reset();
        }

        foreach (var Tuple in Saveables)
        {
            SaveableService SaveableService = Tuple.Value as SaveableService;
            if (SaveableService == null)
                continue;
            if (!SaveableService.IsInit)
                continue;

            SaveableService.OnAfterSaved();
        }

        string FileName = SaveGameName;
        string FullPath = GetSavegamePath() + FileName;
        File.WriteAllBytes(FullPath, Data.ToArray());
        FileInfo fileInfo = new FileInfo(FullPath);
        fileInfo.IsReadOnly = false;
        // @GetMostRecentSave needs this
        fileInfo.CreationTime = DateTime.Now;
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

    private void FindSaveables()
    {
        FoundSaveableServices = new();
        byte[] Data = GetFileData();

        SaveableData.IterateData(Data, 0, Data.Length, out var FoundSaveables);

        foreach (var FoundSaveable in FoundSaveables)
        {
            if (FoundSaveable.Item1 != VariableType.WrapperStart)
                continue;

            int Index = FoundSaveable.Item3 - SaveableData.GetBaseHeaderOffset();
            ReadWrapperTypeHeader(Data, Index, out int _, out SaveGameType Type, out int InnerLength);
            if (!Saveables.ContainsKey(Type))
                continue;

            FoundSaveableServices.Add(Type, Index);
        }
    }

    public override void Reset()
    {
        base.Reset();
        ResetAllServices();
    }

    private void ResetAllServices()
    {
        foreach (var Tuple in Saveables)
        {
            if (!Tuple.Value.IsInit)
                continue;

            Tuple.Value.Reset();
        }
    }

    /** Loads the save and returns true if successful*/
    private bool TryExecuteLoading()
    {
        if (bCreateNewFile)
            return false;

        if (FoundSaveableServices == null)
            return false;

        byte[] Data = GetFileData();
        foreach (var Tuple in FoundSaveableServices)
        {
            SaveGameType Type = Tuple.Key;
            if (!Saveables.TryGetValue(Type, out GameService Service))
                continue;

            if (Service is not SaveableService Saveable)
                continue;

            int Index = Tuple.Value;
            Index = ReadWrapperTypeHeader(Data, Index, out int _, out var _, out int InnerLength);

            Saveable.OnBeforeLoaded();
            Saveable.LoadFrom(Data, Index, Index + InnerLength);
            Saveable.OnAfterLoaded();
        }

        return true;
    }

    //**************************** File Handling *********************************************************

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
        MapContainer Container = Resources.Load("Maps/Tutorial1") as MapContainer;

        string FileName = GetCompleteSaveGameName(TutorialSavegame);
        string Path = GetSavegamePath();
        string FilePath = Path + FileName;

        File.WriteAllBytes(FilePath, Container.MapData);
    }

    private byte[] GetFileData()
    {
        string SaveGame = FileToLoad != null ? FileToLoad : GetMostRecentSave();
        string Filename = GetSavegamePath() + SaveGame;
        return File.ReadAllBytes(Filename);
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


    //**************************** Saving **************************************************************

    private byte[] Save(bool bShouldReset)
    {
        List<byte> Data = new List<byte>();
        foreach (var Tuple in Saveables)
        {
            SaveableService Service = Tuple.Value.GetComponent<SaveableService>();
            Data.AddRange(Service.Save(Tuple.Key, Service.gameObject.name));
            if (!bShouldReset)
                continue;

            Service.Reset();
        }

        return Data.ToArray();
    }

    private void Load(byte[] Data)
    {
        SaveableData.IterateData(Data, 0, Data.Length, out var FoundSaveables);

        foreach (var FoundSaveable in FoundSaveables)
        {
            if (FoundSaveable.Item1 != VariableType.WrapperStart)
                continue;

            int Index = FoundSaveable.Item3 - SaveableData.GetBaseHeaderOffset();
            SaveableService.ReadWrapperTypeHeader(Data, Index, out int _, out SaveGameType Type, out int InnerLength);
            if (!Saveables.ContainsKey(Type))
                continue;

            // skip savegametype and inner length
            int Start = FoundSaveable.Item3 + sizeof(byte) + sizeof(int);
            SaveableService Service = Saveables[Type].GetComponent<SaveableService>();
            Service.LoadFrom(Data, Start, Start + InnerLength);
        }
    }

    //************************* Loading scenes ******************************************
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
}

