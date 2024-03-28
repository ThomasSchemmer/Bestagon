using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/** 
 * Delegates a callback to be executed once other services have been either started or initialized
 * Boilerplate since i cant figure out params[] for templates to allow for variable amount of services
 *
 */
public abstract class GameServiceDelegate
{
    public enum DelegateType
    {
        OnStart,
        OnInit
    }

    public Dictionary<GameService, bool> RequiredServices = new();
    public DelegateType Type;

    public abstract void ExecuteAction();

    protected List<GameService> GetRequiredServices()
    {
        List<GameService> StartedServices = new();
        foreach (var Tuple in RequiredServices)
        {
            if (Tuple.Value)
            {
                StartedServices.Add(Tuple.Key);
            }
        }
        return StartedServices;
    }

    protected void MarkAsReady(GameService Service)
    {
        if (!RequiredServices.ContainsKey(Service))
            return;

        RequiredServices[Service] = true;
        RunIfReady();
    }

    protected void RunIfReady()
    {
        if (!AllServicesReady())
            return;

        ExecuteAction();
        Game.RemoveServiceDelegate(this);
    }

    private bool AllServicesReady()
    {
        foreach (var Tuple in RequiredServices)
        {
            if (!Tuple.Value)
                return false;
        }
        return true;
    }
}

public class GameServiceDelegate<T> : GameServiceDelegate where T : GameService
{
    public Action<T> Action;

    public override void ExecuteAction()
    {
        List<GameService> StartedServices = GetRequiredServices();
        Action((T)StartedServices[0]);
    }

    public GameServiceDelegate(T RequiredService, Action<T> Callback, DelegateType Type = DelegateType.OnStart)
    {
        bool bIsReady = Type == DelegateType.OnStart ? RequiredService.IsRunning : RequiredService.IsInit;
        RequiredServices.Add(RequiredService, bIsReady);
        this.Action = Callback;
        switch (Type)
        {
            case DelegateType.OnStart:
                RequiredService._OnStartup += () =>
                {
                    MarkAsReady(RequiredService);
                };
                break;
            case DelegateType.OnInit:

                RequiredService._OnInit += () =>
                {
                    MarkAsReady(RequiredService);
                };
                break;
        }
        RunIfReady();
    }
}

public class GameServiceDelegate<X, Y> : GameServiceDelegate where X : GameService where Y : GameService
{
    public Action<X, Y> Action;

    public override void ExecuteAction()
    {
        List<GameService> StartedServices = GetRequiredServices();
        Action((X)StartedServices[0], (Y)StartedServices[1]);
    }

    public GameServiceDelegate(X RequiredServiceX, Y RequiredServiceY, Action<X, Y> Callback, DelegateType Type = DelegateType.OnStart)
    {

        bool bIsReadyX = Type == DelegateType.OnStart ? RequiredServiceX.IsRunning : RequiredServiceX.IsInit;
        bool bIsReadyY = Type == DelegateType.OnStart ? RequiredServiceY.IsRunning : RequiredServiceY.IsInit;
        RequiredServices.Add(RequiredServiceX, bIsReadyX);
        RequiredServices.Add(RequiredServiceY, bIsReadyY);
        this.Action = Callback;
        switch (Type)
        {
            case DelegateType.OnStart:
                RequiredServiceX._OnStartup += () =>
                {
                    MarkAsReady(RequiredServiceX);
                };
                RequiredServiceY._OnStartup += () =>
                {
                    MarkAsReady(RequiredServiceY);
                };
                break;
            case DelegateType.OnInit:
                RequiredServiceX._OnInit += () =>
                {
                    MarkAsReady(RequiredServiceX);
                };
                RequiredServiceY._OnInit += () =>
                {
                    MarkAsReady(RequiredServiceY);
                };
                break;
        }
        RunIfReady();
    }
}