using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GameplayAbility", menuName = "ScriptableObjects/GameplayAbility", order = 4)]
public class GameplayAbility : ScriptableObject
{
    public enum State
    {
        Inactive,
        Activated,
        Committed,
        Ended
    }

    public State Status = State.Inactive;
    public GameplayTagRegularContainer AbilityTags = new("Ability Tags");
    public GameplayTagRegularContainer ActivationRequiredTags = new("Activation required Tags");


    public virtual void ActivateAbility()
    {
        Status = State.Activated;
        _OnActivateAbility?.Invoke();
        CommitAbility();
    }

    public virtual void CommitAbility()
    {
        Status = State.Committed;
    }

    public virtual bool CanActivateAbility()
    {
        return true;
    }

    public virtual void EndAbility()
    {
        Status = State.Ended;
        _OnEndAbility?.Invoke();
    }

    public delegate void OnEndAbility();
    public delegate void OnActivateAbility();
    public OnEndAbility _OnEndAbility;
    public OnActivateAbility _OnActivateAbility;
}
