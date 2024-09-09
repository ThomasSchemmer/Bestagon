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

    public GameplayTagRegularContainer GrantedTags = new("Granted Tags");
    public GameplayTagRegularContainer ApplicationRequirementTags = new("Application Requirement Tags");
    public GameplayTagRegularContainer OngoingRequirementTags = new("Ongoing Requirement Tags");
    public GameplayTagRegularContainer RemoveTags = new("Remove Tags");

    public void SetTarget(GameplayAbilityBehaviour Target)
    {
        this.Target = Target;
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

    public GameplayEffect GetByInstancing()
    {
        switch (InstancingPolicy)
        {
            case Instancing.NonInstanced: return this;
            case Instancing.InstancedPerActor:
            case Instancing.InstancedPerExecution: return Instantiate(this);
        }
        return null;
    }


}
