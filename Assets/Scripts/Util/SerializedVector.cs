using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SerializedVector2<T>
{
    [SaveableClass]
    public T x;
    [SaveableClass]
    public T y;

    public SerializedVector2(T x, T y)
    {
        this.x = x;
        this.y = y;
    }

    public SerializedVector2() { }
}
