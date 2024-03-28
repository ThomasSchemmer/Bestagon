using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Game : MonoBehaviour
{
    public GameState State = GameState.GameMenu;
    public GameMode Mode = GameMode.Game;
    public bool bIsPaused = false;
    public int ChunkCount;
    public List<GameServiceWrapper> Services = new();
    private List<GameServiceDelegate> Delegates = new();

    public delegate void OnStateChange(GameState NewState);
    public delegate void OnModeChange(GameMode NewMode);
    public delegate void OnPause();
    public delegate void OnResume();
    public OnStateChange _OnStateChange;
    public OnModeChange _OnModeChange;
    public OnPause _OnPause;
    public OnResume _OnResume;

    public static Game Instance;
    public static string MenuSceneName = "Menu";
    public static string MainSceneName = "Main";
    public static string CardSelectionSceneName = "CardSelection";

    public enum GameState
    {
        Game,
        GameMenu,
        CardSelection,
        MainMenu,
    }

    public enum GameMode
    {
        Game,
        MapEditor
    }

    public void OnEnable()
    {
        Instance = this;
    }

    public void OnOpenMenu()
    {
        bIsPaused = true;
        State = GameState.GameMenu;
        _OnStateChange?.Invoke(State);
        _OnModeChange?.Invoke(Mode);
        _OnPause?.Invoke();
    }

    public void OnCloseMenu()
    {
        bIsPaused = false;
        State = GameState.Game;
        _OnStateChange?.Invoke(State);
        _OnModeChange?.Invoke(Mode);
        _OnResume?.Invoke();
    }

    public void Start()
    {
        if (IngameMenu.Instance)
        {
            IngameMenu.Instance._OnOpenBegin += OnOpenMenu;
            IngameMenu.Instance._OnClose += OnCloseMenu;
        }
        HexagonConfig.mapMaxChunk = ChunkCount;

        InitMode();

        if (IngameMenu.Instance)
        {
            IngameMenu.Instance.Show();
        }
    }

    public void GameOver(string Message = null)
    {
        OnOpenMenu();
        GameOverScreen.GameOver(Message);
    }

    private void InitMode()
    {
        foreach (GameServiceWrapper Wrapper in Services)
        {
            if (ShouldStartService(Wrapper))
            {
                Wrapper.TargetScript.StartService();
            }
            else
            {
                Wrapper.TargetScript.StopService();
            }
        }
    }

    private bool ShouldStartService(GameServiceWrapper Wrapper)
    {
        return (Wrapper.IsForGame && Mode == GameMode.Game) || (Wrapper.IsForEditor && Mode == GameMode.MapEditor);
    }

    public static T GetService<T>() where T : GameService
    {
        if (!Instance)
            return null;

        foreach (GameServiceWrapper Wrapper in Instance.Services)
        {
            if (Wrapper.TargetScript is T)
                return Wrapper.TargetScript as T;
        }
        return null;
    }

    public static bool TryGetService<T>(out T Service, bool ForceLoad = false) where T: GameService
    {
        Service = GetService<T>();
        if (Service == null && ForceLoad)
        {
            throw new Exception("Missing Service: " + typeof(T).ToString());
        }
        return Service != null;
    }

    public static bool TryGetServices<X, Y>(out X ServiceX, out Y ServiceY, bool ForceLoad = false) where X : GameService where Y : GameService
    {
        ServiceX = GetService<X>();
        ServiceY = GetService<Y>();
        if ((ServiceX ==  null || ServiceY == null) && ForceLoad)
        {
            throw new Exception("Missing Service: " + typeof(X).ToString() + " or "+typeof(Y).ToString());
        }
        return ServiceX != null && ServiceY != null;
    }

    public static bool TryGetServices<X, Y, Z>(out X ServiceX, out Y ServiceY, out Z ServiceZ, bool ForceLoad = false) where X : GameService where Y : GameService where Z : GameService
    {
        ServiceX = GetService<X>();
        ServiceY = GetService<Y>();
        ServiceZ = GetService<Z>();
        if ((ServiceX == null || ServiceY == null || ServiceZ == null) && ForceLoad)
        {
            throw new Exception("Missing Service: " + typeof(X).ToString() + " or " + typeof(Y).ToString() + " or " + typeof(Z).ToString());
        }
        return ServiceX != null && ServiceY != null;
    }

    public static void RunAfterServiceStart<T>(Action<T> Callback) where T : GameService
    {
        T Service = GetService<T>();
        if (Service == null)
            return;

        GameServiceDelegate<T> Delegate = new(Service, Callback);
        Instance.Delegates.Add(Delegate);
    }

    public static void RunAfterServiceStart<X, Y>(Action<X, Y> Callback) where X : GameService where Y : GameService
    {
        X ServiceX = GetService<X>();
        Y ServiceY = GetService<Y>();
        if (ServiceX == null || ServiceY == null || !Instance || Callback == null)
            return;

        GameServiceDelegate<X, Y> Delegate = new GameServiceDelegate<X, Y>(ServiceX, ServiceY, Callback);
        Instance.Delegates.Add(Delegate);
    }

    public static void RunAfterServiceInit<T>(Action<T> Callback) where T : GameService
    {
        T Service = GetService<T>();
        if (Service == null)
            return;

        GameServiceDelegate<T> Delegate = new(Service, Callback, GameServiceDelegate.DelegateType.OnInit);
        Instance.Delegates.Add(Delegate);
    }

    public static void RunAfterServiceInit<X, Y>(Action<X, Y> Callback) where X : GameService where Y : GameService
    {
        X ServiceX = GetService<X>();
        Y ServiceY = GetService<Y>();
        if (ServiceX == null || ServiceY == null || !Instance || Callback == null)
            return;

        GameServiceDelegate<X, Y> Delegate = new(ServiceX, ServiceY, Callback, GameServiceDelegate.DelegateType.OnInit);
        Instance.Delegates.Add(Delegate);
    }

    public static void RemoveServiceDelegate(GameServiceDelegate Delegate) { 
        Instance.Delegates.Remove(Delegate);
    }

    /** Marks the given save as to be loaded and transitions to the provided scene
     * If a SaveGameName is not given, either a new savegame will be created or a temp save will be used.
     * This is useful eg when transitioning to the card selection screen, as we dont save everything so no actual
     * savegame should be created
     */
    public static void LoadGame(string SaveGameName, string SceneName, bool bCreateNewGame = false)
    {
        if (!TryGetService(out SaveGameManager Manager))
            return;

        if (bCreateNewGame && SaveGameName != null)
        {
            bCreateNewGame = false;
        }

        Manager.MarkSaveForLoading(SaveGameName, bCreateNewGame);
        
        AsyncOperation Op = SceneManager.LoadSceneAsync(SceneName);
        Op.allowSceneActivation = true;
    }

    public static void ExitGame()
    {
        if (Application.isEditor)
        {
            EditorApplication.isPlaying = false;
        }
        else
        {
            Application.Quit();
        }
    }

    // todo: make states actually meaningful
    public static bool IsIn(GameState TargetState)
    {
        if (!Instance)
            return false;

        return Instance.State == TargetState;
    }

}
