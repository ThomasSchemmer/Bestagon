using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
/** 
 * Represents a single user preference, used in @Settings
 * Can't be abstract as the List PropertyField would not be found
 */
public class Setting
{
    public enum Type
    {
        Boolean,
        Int
    }

    public Setting(Type Type)
    {
        this._Type = Type;
        Value = 0;
    }

    public Type _Type;
    // needs to be casted everytime! Unity doesn't like custom editors with generic classes
    public int Value;
    public int MinValue;
    public int MaxValue;
}
