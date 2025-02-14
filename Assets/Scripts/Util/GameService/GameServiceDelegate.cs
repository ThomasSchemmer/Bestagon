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

    protected Dictionary<GameService, bool> RequiredServices = new();
    protected DelegateType Type;
    protected bool bHasRun = false;

    public abstract void ExecuteAction();
    public abstract void ResetAction();

    public abstract bool TryGetActionTarget(out GameService Target);

    public List<GameService> GetRequiredServices()
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
    }

    protected virtual void ResetDelegates()
    {
        // reset everything to avoid multiple triggers
        foreach (GameService Service in GetRequiredServices())
        {
            switch (Type)
            {
                case DelegateType.OnStart:
                    Service._OnStartup -= MarkAsReady;
                    break;
                case DelegateType.OnInit:
                    Service._OnInit -= MarkAsReady;
                    break;
            }
        }
        Game.RemoveServiceDelegate(this);
    }

    public bool HasRun()
    {
        return bHasRun;
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
        if (Action == null)
            return;

        List<GameService> StartedServices = GetRequiredServices();
        bHasRun = true;
        ResetDelegates();
        Action((T)StartedServices[0]);
        ResetAction();
    }

    public override void ResetAction()
    {
        Action = null;
    }

    public override bool TryGetActionTarget(out GameService Target)
    {
        Target = null;
        if (Action.Target is not GameService)
            return false;

        Target = (GameService)Action.Target;
        return true;
    }

    public GameServiceDelegate(T RequiredService, Action<T> Callback, DelegateType Type = DelegateType.OnStart)
    {
        bool bIsReady = Type == DelegateType.OnStart ? RequiredService.IsRunning : RequiredService.IsInit;
        RequiredServices.Add(RequiredService, bIsReady);
        this.Action = Callback;
        this.Type = Type;
        switch (Type)
        {
            case DelegateType.OnStart:
                RequiredService._OnStartup += MarkAsReady;
                break;
            case DelegateType.OnInit:
                RequiredService._OnInit += MarkAsReady;
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
        if (Action == null)
            return;

        List<GameService> StartedServices = GetRequiredServices();
        bHasRun = true;
        ResetDelegates();
        Action((X)StartedServices[0], (Y)StartedServices[1]);
        ResetAction();
    }

    public override void ResetAction()
    {
        Action = null;
    }

    public override bool TryGetActionTarget(out GameService Target)
    {
        Target = null;
        if (Action == null || Action.Target is not GameService)
            return false;

        Target = (GameService)Action.Target;
        return true;
    }

    public GameServiceDelegate(X RequiredServiceX, Y RequiredServiceY, Action<X, Y> Callback, DelegateType Type = DelegateType.OnStart)
    {
        bool bIsReadyX = Type == DelegateType.OnStart ? RequiredServiceX.IsRunning : RequiredServiceX.IsInit;
        bool bIsReadyY = Type == DelegateType.OnStart ? RequiredServiceY.IsRunning : RequiredServiceY.IsInit;
        RequiredServices.Add(RequiredServiceX, bIsReadyX);
        RequiredServices.Add(RequiredServiceY, bIsReadyY);
        this.Action = Callback;
        this.Type = Type;


        List<GameService> Services = new()
        {
            RequiredServiceX,
            RequiredServiceY
        };
        foreach (GameService Service in Services)
        {
            switch (Type)
            {
                case DelegateType.OnStart:
                    Service._OnStartup += MarkAsReady;
                    break;
                case DelegateType.OnInit:
                    Service._OnInit += MarkAsReady;
                    break;
            }
        }
        RunIfReady();
    }
}