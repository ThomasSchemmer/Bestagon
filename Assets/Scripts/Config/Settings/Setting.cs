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
    public enum SettingType
    {
        boolean
    }

    public string Name;

    public virtual object GetValue() { return null; }
    public virtual void SetValue(object Value) { }
}

[Serializable]
public abstract class Setting<T> : Setting
{
    public T Value;

    public override object GetValue()
    {
        return Value;
    }

    public override void SetValue(object Value)
    {
        if (Value is not T)
            return;

        this.Value = (T)Value;
    }
}

[Serializable]
public class BooleanSetting: Setting<bool>
{

}

