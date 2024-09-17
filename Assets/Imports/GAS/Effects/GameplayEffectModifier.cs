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

    public AttributeType AttributeType;
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

        _Attribute = Target.Attributes[AttributeType];
    }

    public void Execute()
    {
        _Attribute.AddModifier(this);
    }

    public void Revert()
    {
        _Attribute.AddModifier(Invert());
    }

    private GameplayEffectModifier Invert()
    {
        if (Operation == Type.Override)
            return null;

        GameplayEffectModifier Copy = new();
        Copy.Period = this.Period;
        Copy.AttributeType = this.AttributeType;
        Copy.SetTarget(this.Target);
        Copy.Operation = this.Operation;

        switch (Operation)
        {
            case Type.Add: Copy.Value = -this.Value; break;
            case Type.Multiply: 
            case Type.Divide: Copy.Value = 1 / this.Value; break;
            default: break;
        }

        return Copy;
    }

    public void Tick(float Delta)
    {
        TimeSinceLastActivated += Delta;
        if (TimeSinceLastActivated < Period)
            return;

        TimeSinceLastActivated -= Period;
        _Attribute.AddModifier(this);
    }

    public string GetDescription()
    {
        return GetOperationDescription() +
            GetAttributeDescription() + 
            GetOperationPrepositionDescription() +
            Value;
    }

    // https://stackoverflow.com/a/6137889
    private string GetAttributeDescription()
    {
        string Name = AttributeType.ToString();
        return System.Text.RegularExpressions.Regex.Replace(Name, "([A-Z])", " $1", System.Text.RegularExpressions.RegexOptions.Compiled).Trim();
    }

    private string GetOperationDescription()
    {
        switch (Operation)
        {
            case Type.Add: return "Increases ";
            case Type.Multiply: return "Multiplies ";
            case Type.Divide: return "Divides ";
            case Type.Override: return "Overrides ";
            default: return "";
        }
    }

    private string GetOperationPrepositionDescription()
    {
        switch (Operation)
        {
            case Type.Add: return " by ";
            case Type.Multiply: return " by ";
            case Type.Divide: return " by ";
            case Type.Override: return " with ";
            default: return "";
        }
    }

}
