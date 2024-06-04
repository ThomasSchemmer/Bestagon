using System;

[Serializable]
public class FloatRange 
{
    public float Min;
    public float Max;

    public FloatRange(float min, float max)
    {
        Min = min;
        Max = max;
    }

    public FloatRange() { }

    public bool Contains(float value)
    {
        return value >= Min && value <= Max;
    }

    public float GetMidPoint()
    {
        return (Min + Max) / 2;
    }
}
