using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GameplayEffectModifier
{
    public enum Type
    {
        Add,
        Multiply,
        Divide,
        Override
    }

    public string Attribute;
    private Attribute _Attribute;
    public Type Operation;
    public float Period = 0;
    public float Value = 0;

    private float TimeSinceLastActivated = -1;

    private GameplayAbilityBehaviour Target;

    public void SetTarget(GameplayAbilityBehaviour Target)
    {
        this.Target = Target;
        TimeSinceLastActivated = 0;

        Target.Attributes.TryFind(Attribute, out _Attribute);
    }

    public void Execute()
    {
        _Attribute.AddModifier(this);
    }

    public void Tick(float Delta)
    {
        TimeSinceLastActivated += Delta;
        if (TimeSinceLastActivated < Period)
            return;

        TimeSinceLastActivated -= Period;
        _Attribute.AddModifier(this);
    }

}
