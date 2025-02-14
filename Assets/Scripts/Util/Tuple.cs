using System;
using UnityEngine;
using UnityEngine.UIElements;

[Serializable]
public class Tuple<TType, TValue>
{
    public Tuple(TType Key, TValue Value)
    {
        this.Key = Key;
        this.Value = Value;
    }

    public TType Key;
    public TValue Value;
}

public class Climate : Tuple<float, float>
{
    public Climate(float Key, float Value) : base(Key, Value){}
    public Climate(Vector2 Point) : base(Point.x, Point.y) {}

    public Vector2 Point
    {
        get { return new Vector2(Key, Value); }
    }
}