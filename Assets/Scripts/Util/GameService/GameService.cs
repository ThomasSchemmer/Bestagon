
using System;
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
        _OnStartup?.Invoke();
    }

    public void StopService()
    {
        IsRunning = false;
        StopServiceInternal();
        _OnShutdown?.Invoke();
    }

    private void BaseInit()
    {
        IsInit = true;
    }

    protected abstract void StartServiceInternal();
    protected abstract void StopServiceInternal();

    public delegate void OnStartup();
    public delegate void OnShutdown();
    public delegate void OnInit();
    public OnStartup _OnStartup;
    public OnShutdown _OnShutdown;
    /** Needs to be manually called whenever the service is fully initialized - not all services will call it*/
    public OnInit _OnInit;

    public bool IsRunning = false;
    public bool IsInit = false;
}
