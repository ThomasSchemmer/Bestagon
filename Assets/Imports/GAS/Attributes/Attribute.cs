using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Attribute
{
    public string Name = "";
    public float BaseValue = 0;
    public float CurrentValue = 0;

    public Attribute()
    {
        Modifiers = new();
        ResetModifiers();
        Initialize();
    }

    public void Initialize()
    {
        CurrentValue = BaseValue;
    }

    private void ResetModifiers()
    {
        Modifiers.Remove(GameplayEffectModifier.Type.Add);
        Modifiers.Remove(GameplayEffectModifier.Type.Multiply);
        Modifiers.Remove(GameplayEffectModifier.Type.Divide);
        Modifiers.Remove(GameplayEffectModifier.Type.Override);
        Modifiers.Add(GameplayEffectModifier.Type.Add, new());
        Modifiers.Add(GameplayEffectModifier.Type.Multiply, new());
        Modifiers.Add(GameplayEffectModifier.Type.Divide, new());
        Modifiers.Add(GameplayEffectModifier.Type.Override, new());
    }

    public void Tick()
    {
        float OldValue = CurrentValue;
        float DivideValue = 1 + Divide;
        float MultiplyValue = 1 + Multiply;
        CurrentValue = (CurrentValue + Add) * MultiplyValue / DivideValue;
        if (!Mathf.Approximately(Override, 0))
        {
            CurrentValue = Override;
        }

        if (!Mathf.Approximately(OldValue, CurrentValue))
        {
            _OnAttributeChanged?.Invoke();
        }

        ResetModifiers();
    }

    public void AddModifier(GameplayEffectModifier Modifier)
    {
        if (!Modifiers.TryGetValue(Modifier.Operation, out List<GameplayEffectModifier> TargetList))
            return;

        TargetList.Add(Modifier);
    }

    private float GetModifiedValueFor(GameplayEffectModifier.Type Operation)
    {
        if (!Modifiers.TryGetValue(Operation, out List<GameplayEffectModifier> TargetList))
            return 0;

        float Result = 0;
        foreach (GameplayEffectModifier Modifier in TargetList)
        {
            Result += Modifier.Value;
        }

        return Result;
    }

    private float Add
    {
        get { return GetModifiedValueFor(GameplayEffectModifier.Type.Add); }
    }

    private float Multiply
    {
        get { return GetModifiedValueFor(GameplayEffectModifier.Type.Multiply); }
    }

    private float Divide
    {
        get { return GetModifiedValueFor(GameplayEffectModifier.Type.Divide); }
    }

    private float Override
    {
        get { return GetModifiedValueFor(GameplayEffectModifier.Type.Override); }
    }

    Dictionary<GameplayEffectModifier.Type, List<GameplayEffectModifier>> Modifiers;

    public delegate void OnAttributeChanged();
    public OnAttributeChanged _OnAttributeChanged;
}
