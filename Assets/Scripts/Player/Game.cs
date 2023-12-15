using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using static Game;
using static GameService;

public class Game : MonoBehaviour
{
    public GameState State = GameState.Paused;
    public GameMode Mode = GameMode.Menu;
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

    private GameMode OldMode = GameMode.Menu;

    public enum GameState
    {
        Playing,
        Paused,
    }

    public enum GameMode
    {
        Menu = 0,
        Game,
        MapEditor
    }

    public void OnEnable()
    {
        Instance = this;
    }

    public void OnOpenMenu()
    {
        State = GameState.Paused;
        OldMode = Mode;
        Mode = GameMode.Menu;
        _OnStateChange?.Invoke(State);
        _OnModeChange?.Invoke(Mode);
        _OnPause?.Invoke();
    }

    public void OnCloseMenu()
    {
        State = GameState.Playing;
        Mode = OldMode;
        _OnStateChange?.Invoke(State);
        _OnModeChange?.Invoke(Mode);
        _OnResume?.Invoke();
    }

    public void Start()
    {
        MainMenu.Instance._OnOpenBegin += OnOpenMenu;
        MainMenu.Instance._OnClose += OnCloseMenu;
        HexagonConfig.SetChunkCount(ChunkCount);

        InitMode();

        MainMenu.Instance.Show();
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

    public static bool TryGetService<T>(out T Service) where T: GameService
    {
        Service = GetService<T>();
        return Service != null;
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

    public static void RemoveServiceDelegate(GameServiceDelegate Delegate) { 
        Instance.Delegates.Remove(Delegate);
    }

}
