using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Game : MonoBehaviour
{
    public GameState State = GameState.GameMenu;
    public GameMode Mode = GameMode.Game;
    public bool bIsPaused = false;
    public int TargetFramerate = 60;
    public List<GameServiceWrapper> Services = new();
    private List<GameServiceDelegate> Delegates = new();
    private Dictionary<Type, GameServiceWrapper> ServicesInternal = new();

    private Dictionary<GameService, HashSet<GameService>> CallbackMap = new();

    public delegate void OnStateChange(GameState NewState);
    public delegate void OnModeChange(GameMode NewMode);
    public delegate void OnPause();
    public delegate void OnResume();
    public delegate void OnPopup(bool bIsOpen);
    public OnStateChange _OnStateChange;
    public OnModeChange _OnModeChange;
    public OnPause _OnPause;
    public OnResume _OnResume;
    public OnPopup _OnPopup;

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
    public void OnPopupAction(bool bIsOpen)
    {
        _OnPopup?.Invoke(bIsOpen);
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
        Application.targetFrameRate = TargetFramerate;
        if (IngameMenuScreen.Instance)
        {
            IngameMenuScreen.Instance._OnOpenBegin += OnOpenMenu;
            IngameMenuScreen.Instance._OnClose += OnCloseMenu;
        }

        ConvertToDictionary();
        InitMode();
    }

    public void Update()
    {
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.P))
        {
            EditorApplication.isPaused = !EditorApplication.isPaused;
        }
#endif
    }

    public void GameOver(string Message = null)
    {
        OnOpenMenu();
        GameOverScreen.GameOver(Message);
    }

    private void ConvertToDictionary()
    {
        foreach (GameServiceWrapper Wrapper in Services)
        {
            Type Type = Wrapper.TargetScript.GetType();
            ServicesInternal.Add(Type, Wrapper);
        }
    }

    private void InitMode()
    {
        foreach (GameServiceWrapper Wrapper in ServicesInternal.Values)
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

        if (!Instance.ServicesInternal.ContainsKey(typeof(T)))
            return null;

        return Instance.ServicesInternal[typeof(T)].TargetScript as T;
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
        if (Delegate.HasRun())
            return;

        Instance.Delegates.Add(Delegate);
        Instance.RegisterCallback(Callback.Target, Service);
    }

    public static void RunAfterServicesInit<X, Y>(Action<X, Y> Callback) where X : GameService where Y : GameService
    {
        X ServiceX = GetService<X>();
        Y ServiceY = GetService<Y>();
        if (ServiceX == null || ServiceY == null || !Instance || Callback == null)
            return;

        GameServiceDelegate<X, Y> Delegate = new(ServiceX, ServiceY, Callback, GameServiceDelegate.DelegateType.OnInit);
        Instance.Delegates.Add(Delegate);

        Instance.RegisterCallback(Callback.Target, ServiceX);
        Instance.RegisterCallback(Callback.Target, ServiceY);
    }

    private void RegisterCallback(object ObjectA, GameService B)
    {
        if (ObjectA is not GameService)
            return;

        GameService A = (GameService)ObjectA;
        if (!A)
            return;

        if (CheckForAnyLoopBetween(A, B, out List<GameService> Chain))
        {
            throw new Exception("Infinite wait-for-callback loop between: " + A.name + " and " + B.name);
        }

        if (!CallbackMap.ContainsKey(A))
        {
            CallbackMap[A] = new();
        }
        CallbackMap[A].Add(B);
    }

    private void RemoveCallback(GameService A, GameService B)
    {
        if (!CallbackMap.ContainsKey(A))
            return;

        if (!CallbackMap[A].Contains(B))
            return;

        CallbackMap[A].Remove(B);
    }

    private bool CheckForAnyLoopBetween(GameService A, GameService B, out List<GameService> Chain)
    {
        Chain = new();

        if (A == B)
        {
            Chain.Add(A);
            return true;
        }

        if (!CallbackMap.ContainsKey(B))
            return false;

        foreach (var OtherService in CallbackMap[B])
        {
            if (!CheckForAnyLoopBetween(A, OtherService, out List<GameService> PrevChain))
                continue;

            Chain.Add(B);
            Chain.AddRange(PrevChain);
            return true;
        }
        return false;
    }

    public static void RemoveServiceDelegate(GameServiceDelegate Delegate) { 
        Instance.Delegates.Remove(Delegate);
        if (!Delegate.TryGetActionTarget(out GameService A))
            return;

        foreach (var OtherService in Delegate.GetRequiredServices())
        {
            Instance.RemoveCallback(A, OtherService);
        }
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
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }

    // todo: make states actually meaningful
    public static bool IsIn(GameState TargetState)
    {
        if (!Instance)
            return false;

        return Instance.State == TargetState;
    }

}
