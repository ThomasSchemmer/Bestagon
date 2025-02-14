using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GameplayEffect", menuName = "ScriptableObjects/GameplayEffect", order = 3)]
public class GameplayEffect : ScriptableObject
{
    public enum Duration
    {
        Instant, 
        Duration,
        Infinite
    }

    public enum Instancing
    {
        InstancedPerExecution,
        InstancedPerActor,
        NonInstanced
    }

    public Duration DurationPolicy;
    public Instancing InstancingPolicy;
    public List<GameplayEffectModifier> Modifiers = new();
    public float DurationLength = 0;
    public GameplayAbility GrantedAbility;

    private GameplayAbilityBehaviour Target;
    private float Runtime = 0;
    private bool bIsDeactivated = false;

    public GameplayTagRegularContainer GrantedTags = new("Granted Tags");
    public GameplayTagRegularContainer ApplicationRequirementTags = new("Application Requirement Tags");
    public GameplayTagRegularContainer OngoingRequirementTags = new("Ongoing Requirement Tags");
    public GameplayTagRegularContainer RemoveTags = new("Remove Tags");

    public void SetTarget(GameplayAbilityBehaviour Target)
    {
        this.Target = Target;
        bIsDeactivated = false;
        Runtime = 0;
        foreach (GameplayEffectModifier Modifier in Modifiers)
        {
            Modifier.SetTarget(Target);
        }
    }

    public void Execute()
    {
        if (DurationPolicy != Duration.Instant)
            return;

        foreach (GameplayEffectModifier Modifier in Modifiers) { 
            Modifier.Execute(); 
        }

        Target.RemoveTags(RemoveTags.IDs);
    }

    public void Deactivate()
    {
        if (DurationPolicy != Duration.Instant)
            return;

        foreach (GameplayEffectModifier Modifier in Modifiers)
        {
            Modifier.Revert();
        }
        bIsDeactivated = true;
    }

    public void Tick(float Delta)
    {
        if (DurationPolicy == Duration.Instant)
            return;

        foreach (GameplayEffectModifier Modifier in Modifiers)
        {
            Modifier.Tick(Delta);
        }
    }

    public bool IsExpired(float Delta)
    {
        if (bIsDeactivated)
            return true;

        if (DurationPolicy == Duration.Instant)
            return false;

        Runtime += Delta;
        return Runtime > DurationLength;
    }

    public List<GameplayEffectModifier> GetModifiersByOperation(GameplayEffectModifier.Type Operation)
    {
        List<GameplayEffectModifier> SelectedModifiers = new();
        foreach (GameplayEffectModifier Modifier in Modifiers)
        {
            if (Modifier.Operation != Operation)
                continue;

            SelectedModifiers.Add(Modifier);
        }
        return SelectedModifiers;
    }

    public GameplayEffect GetByInstancing(GameplayAbilityBehaviour Target)
    {
        switch (InstancingPolicy)
        {
            case Instancing.NonInstanced: return this;
            case Instancing.InstancedPerActor: return GetByInstancingActor(Target);
            case Instancing.InstancedPerExecution: return Instantiate(this);
        }
        return null;
    }

    private GameplayEffect GetByInstancingActor(GameplayAbilityBehaviour Target)
    {
        if (Target.GetActiveEffects().Contains(this))
            return this;

        return Instantiate(this);
    }

    public string GetEffectDescription()
    {
        string Result = "";
        for (int i = 0; i < Modifiers.Count; i++)
        {
            Result += Modifiers[i].GetDescription();
            if (i == Modifiers.Count - 1)
                continue;
            Result += "\n"; 
        }
        return Result;
    }
}
