
using System;
using UnityEditor;
using UnityEngine;

/**
 * Abstract class to allow the GameService to start / stop the attached script
 */
public abstract class GameService : MonoBehaviour
{
    public GameService()
    {
        // has to be in the constructor to force the self initialisation to be the first thing executed on callback
        _OnInit += BaseInit;
    }

    public void StartService()
    {
        IsRunning = true;
        StartServiceInternal();
        _OnStartup?.Invoke(this);
    }

    public void StopService()
    {
        IsRunning = false;
        StopServiceInternal();
        _OnShutdown?.Invoke(this);
    }

    private void BaseInit(GameService Service)
    {
        IsInit = true;
    }

    /** Deletes all internal data so that it can be loaded from the save or initialized safely*/
    protected abstract void ResetInternal();

    public void Reset()
    {
        ResetInternal();
        IsInit = false;
    }

    protected abstract void StartServiceInternal();
    protected abstract void StopServiceInternal();

    public delegate void OnStartup(GameService Service);
    public delegate void OnShutdown(GameService Service);
    public delegate void OnInit(GameService Service);
    public OnStartup _OnStartup;
    public OnShutdown _OnShutdown;
    /** Needs to be manually called whenever the service is fully initialized - not all services will call it*/
    public OnInit _OnInit;

    public bool IsRunning = false;
    public bool IsInit = false;
}
