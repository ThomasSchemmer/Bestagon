using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
/** 
 * Wrapper class for the actual service script
 * Unity display / serialization for lists is weird if you want to edit a monobehaviour drawer
 */
public class GameServiceWrapper
{
    public bool IsForEditor = false;
    public bool IsForGame = false;

    public GameService TargetScript;
}
