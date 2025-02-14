using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Workers;

/** Core component of the GAS, handles communication between different GAB's */
public class GameplayAbilitySystem : GameService
{
    public List<GameplayAbilityBehaviour> Behaviours = new();

    public void Update()
    {
        foreach (var Behaviour in Behaviours)
        {
            Behaviour.Tick(Time.deltaTime);
        }
    }

    public void Register(GameplayAbilityBehaviour Behaviour)
    {
        Behaviours.Add(Behaviour);
        _OnBehaviourRegistered?.Invoke(Behaviour);
    }

    protected override void ResetInternal()
    {
        
        AttributeSet.Get().Reset();
    }

    protected override void StartServiceInternal()
    {
        _OnInit?.Invoke(this);
    }

    protected override void StopServiceInternal() {}

    public bool TryApplyEffectTo(GameplayAbilityBehaviour Target, GameplayEffect Effect)
    {
        if (Target == null)
            return false;

        if (!Target.HasTags(Effect.ApplicationRequirementTags.IDs))
            return false;

        GameplayEffect Clone = Effect.GetByInstancing(Target);

        Clone.SetTarget(Target);
        Target.AddEffect(Clone);
        return true;
    }

    public bool TryActivateAbility(GameplayAbilityBehaviour Target, GameplayAbility Ability)
    {
        if (!Target.HasTags(Ability.ActivationRequiredTags.IDs))
            return false;

        if (!Target.HasAbility(Ability))
        {
            Target.GrantAbility(Ability);
        }

        Ability.ActivateAbility();
        return true;
    }

    public delegate void OnBehaviourRegistered(GameplayAbilityBehaviour Behavior);
    public static OnBehaviourRegistered _OnBehaviourRegistered;
}
