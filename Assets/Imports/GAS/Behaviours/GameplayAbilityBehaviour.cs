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
            Attributes.Initialize();
            bIsInitialized = true;
            HandleDelayedEffects();
        });
    }

    public void Tick(float Delta)
    {
        if (!bIsInitialized)
            return;

        // Effects can register themselves on attributes
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
        MarkedForRemovalEffects.Clear();

        // now attributes can be calculated
        Attributes.Tick();
    }

    private void HandleDelayedEffects()
    {
        if (!bIsInitialized)
        {
            throw new System.Exception("Cannot handle delayed effects if not fully initialized!");
        }

        foreach (var Tuple in EffectsToHandle)
        {
            if (Tuple.Value)
            {
                AddEffect(Tuple.Key);
            }
            else
            {
                RemoveEffect(Tuple.Key);
            }
        }
        EffectsToHandle.Clear();
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
        if (!bIsInitialized)
        {
            EffectsToHandle.Add(new(Effect, true));
            return;
        }

        ActiveEffects.Add(Effect);
        AddTagsByID(Effect.GrantedTags.IDs);
        Effect.Execute();

        if (Effect.GrantedAbility == null || Effect.DurationPolicy == GameplayEffect.Duration.Instant)
            return;

        GrantAbility(Effect.GrantedAbility);
    }

    public List<GameplayEffect> GetActiveEffects()
    {
        return ActiveEffects;
    }
       

    public void RemoveEffect(GameplayEffect Effect)
    {
        if (!bIsInitialized)
        {
            EffectsToHandle.Add(new(Effect, false));
            return;
        }

        // have to check that after the queue, as it might be slightly before (but still in the queue)
        if (!ActiveEffects.Contains(Effect))
            return;

        ActiveEffects.Remove(Effect);
        RemoveTags(Effect.GrantedTags.IDs);
        Effect.Deactivate();

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

    private bool bIsInitialized = false;

    private List<Tuple<GameplayEffect, bool>> EffectsToHandle = new();

    public delegate void OnTagsChanged();
    public delegate void OnTagAdded(Guid Tag);
    public delegate void OnTagRemoved(Guid Tag);
    public OnTagsChanged _OnTagsChanged;
    public OnTagAdded _OnTagAdded;
    public OnTagRemoved _OnTagRemoved;
}
