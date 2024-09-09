using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameplayAbilityBehaviour : MonoBehaviour
{
    void Start()
    {
        Game.RunAfterServiceInit((GameplayAbilitySystem System) =>
        {
            System.Register(this);
        });
        Attributes.Initialize();
    }

    public void Tick(float Delta)
    {
        // Effects can register themselfs on attributes
        foreach (GameplayEffect ActiveEffect in ActiveEffects)
        {
            bool bIsExpired = ActiveEffect.IsExpired(Delta);
            bool bHasTags = HasTags(ActiveEffect.OngoingRequirementTags.IDs);
            if (!bHasTags || bIsExpired)
            {
                MarkedForRemovalEffects.Add(ActiveEffect);
                continue;
            }

            ActiveEffect.Tick(Delta);
        }
        foreach (GameplayEffect ToRemoveEffect in MarkedForRemovalEffects)
        {
            RemoveEffect(ToRemoveEffect);
        }

        // now attributes can be calculated
        Attributes.Tick();
    }

    public void AddTagByID(Guid ID) {
        GameplayTagMask.Set(ID);

        _OnTagsChanged?.Invoke();
        _OnTagAdded?.Invoke(ID);
    }

    public void AddTagsByID(List<Guid> IDs)
    {
        foreach (Guid ID in IDs)
        {
            AddTagByID(ID);
        }
    }

    public void RemoveTag(Guid ID)
    {
        GameplayTagMask.Clear(ID);

        _OnTagsChanged?.Invoke();
        _OnTagAdded?.Invoke(ID);
    }

    public void RemoveTags(List<Guid> IDs)
    {
        foreach (Guid ID in IDs)
        {
            RemoveTag(ID);
        }
    }

    public bool HasTag(Guid Tag)
    {
        return GameplayTagMask.HasID(Tag);
    }

    public bool HasTags(List<Guid> IDs)
    {
        foreach (var ID in IDs)
        {
            if (!HasTag(ID))
                return false;
        }
        return true;
    }

    public void AddEffect(GameplayEffect Effect) { 
        ActiveEffects.Add(Effect);
        AddTagsByID(Effect.GrantedTags.IDs);

        if (Effect.GrantedAbility == null || Effect.DurationPolicy == GameplayEffect.Duration.Instant)
            return;

        GrantAbility(Effect.GrantedAbility);
    }

    public void RemoveEffect(GameplayEffect Effect)
    {
        ActiveEffects.Remove(Effect);
        RemoveTags(Effect.GrantedTags.IDs);

        if (Effect.GrantedAbility == null || Effect.DurationPolicy == GameplayEffect.Duration.Instant)
            return;

        GrantedAbilities.Remove(Effect.GrantedAbility);
    }

    public void GrantAbility(GameplayAbility Ability)
    {
        GrantedAbilities.Add(Ability);
    }

    public bool HasAbility(GameplayAbility Ability)
    {
        return GrantedAbilities.Contains(Ability);
    }

    public void RemoveAbility(GameplayAbility Ability)
    {
        GrantedAbilities.Remove(Ability);
    }

    public static GameplayAbilityBehaviour Get(GameObject GameObject)
    {
        return GameObject.GetComponent<GameplayAbilityBehaviour>();
    }

    public AttributeSet Attributes;
    private List<GameplayEffect> ActiveEffects = new();
    private List<GameplayEffect> MarkedForRemovalEffects = new();
    private List<GameplayAbility> GrantedAbilities = new();
    private GameplayTagMask GameplayTagMask = new();

    public delegate void OnTagsChanged();
    public delegate void OnTagAdded(Guid Tag);
    public delegate void OnTagRemoved(Guid Tag);
    public OnTagsChanged _OnTagsChanged;
    public OnTagAdded _OnTagAdded;
    public OnTagRemoved _OnTagRemoved;
}
