using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;

/** 
 * Regulates unlocking of different things, should always be contained in a @IUnlockableService 
 * that handles the calling of methods / saving
 */
public abstract class Unlockables
{
    public enum State : uint
    {
        Locked = 0,
        Unlocked = 1,
        Active = 2
    }
}

/**
 * Templated version that allows for variable types
 * Contains the actual categories that can be unlock (incl the types)
 * Note: boilerplate code to allow T to be an enum inheriting uint
 */
public class Unlockables<T> : Unlockables, IQuestRegister<T> where T : struct, IConvertible
{
    private IUnlockableService<T> Service;
    [SaveableBaseType]
    private Type ServiceType;
    [SaveableList]
    private List<SerializedDictionary<T, State>> Categories;

    public void Init(IUnlockableService<T> Service)
    {
        this.Service = Service;
        this.ServiceType = Service.GetType();
        this.Categories = new();
        this.Service.InitUnlockables();
    }

    public bool TryUnlockNewType(int Seed, out T Type, bool bIsPreview = false)
    {
        Type = default;
        if (!Service.IsInit())
            return false;

        Type = GetRandomOfState(Seed, State.Locked, false, true);

        if (!bIsPreview)
        {
            this[Type] = State.Unlocked;
        }
        return true;
    }

    public void MarkAs(T Type, int CategoryIndex, State NewState) 
    {
        State OldState = this[Type];
        if (OldState == default)
            return;

        this[Type] = NewState;

        if (OldState != NewState)
        {
            _OnStateChanged.ForEach(_ => _.Invoke(Type, NewState));
            _OnTypeChanged.ForEach(_ => _.Invoke(Type));
        }
    }

    public bool IsLocked(T Type)
    {
        return this[Type] <= State.Locked;
    }

    public T GetRandomOfState(int Seed, State TargetState, bool bCanBeHigher, bool bSingleCategory)
    {
        List<int> TargetCategories = new();
        for (int i = 0; i < Categories.Count; i++)
        {
            if (!HasCategoryAnyInState(i, TargetState, bCanBeHigher))
                continue;

            TargetCategories.Add(i);
            if (bSingleCategory)
                break;
        }

        UnityEngine.Random.InitState(Seed);
        int RandomCategory = UnityEngine.Random.Range(0, TargetCategories.Count);
        var SelectedCategory = Categories[TargetCategories[RandomCategory]];

        List<T> TargetTypes = new();
        foreach(var Tuple in SelectedCategory)
        {
            if (!bCanBeHigher && Tuple.Value != TargetState)
                continue;

            if (bCanBeHigher && Tuple.Value < TargetState)
                continue;

            TargetTypes.Add(Tuple.Key);
        }

        int RandomType = UnityEngine.Random.Range(0, TargetTypes.Count);
        return TargetTypes[RandomType];
    }

    public void UnlockCategory(T CategoryType, int MaxIndex)
    {
        int Mask = Service.GetValueAsInt(CategoryType);
        for (int i = 0; i <= MaxIndex; i++)
        {
            if ((Mask & (1 << i)) == 0)
                continue;

            this[Service.GetValueAsT(1 << i)] = State.Unlocked;
        }
    }

    public T GetCategoryMask(int CategoryIndex)
    {
        T Mask = default;
        for (int i = 0; i < Categories[CategoryIndex].Count; i++)
        {
            T Key = Categories[CategoryIndex].GetKeyAt(i);
            Mask = Service.Combine(Mask, Key);
        }
        return Mask;
    }

    public T[] GetCategoryMasks()
    {
        T[] Masks = new T[Categories.Count];
        for (int i =0; i < Categories.Count; i++)
        {
            Masks[i] = GetCategoryMask(i);
        }
        return Masks;
    }

    public bool HasCategoryAllUnlocked(int CategoryIndex)
    {
        foreach (var Tuple in Categories[CategoryIndex])
        {
            if (Tuple.Value == State.Locked)
                return false;
        }
        return true;
    }

    public int GetCategoryIndexOf(T Unlockable)
    {
        for (int i = 0; i < Categories.Count; i++)
        {
            if (Categories[i].ContainsKey(Unlockable))
                return i;
        }
        return -1;
    }

    private bool HasCategoryAnyInState(int CategoryIndex, State TargetState, bool bCanBeHigher)
    {
        foreach (var Tuple in Categories[CategoryIndex])
        {
            if (!bCanBeHigher && Tuple.Value == TargetState)
                return true;

            if (bCanBeHigher && Tuple.Value >= TargetState)
                return true;
        }
        return false;
    }

    private int GetCategoriesAmount()
    {
        int Count = 0;
        for (int i = 0; i < Categories.Count; i++)
        {
            Count += Categories[i].Count;
        }
        return Count;
    }

    public void OnLoaded()
    {
        Service = Game.GetService(ServiceType) as IUnlockableService<T>;
        // trigger the registering of newly loaded unlockables
        Service.OnLoadedUnlockables();
    }

    public void Reset()
    {
        if (Service == null)
            return;

        Service.InitUnlockables();
    }

    private void Set(T Type, State Value)
    {
        for (int i = 0; i < Categories.Count; i++)
        {
            if (!Categories[i].ContainsKey(Type))
                continue;

            Categories[i][Type] = Value;
        }
    }

    private State Get(T Type)
    {
        if (Categories == null)
            return default;

        for (int i = 0; i < Categories.Count; i++)
        {
            if (!Categories[i].ContainsKey(Type))
                continue;

            return Categories[i][Type];
        }
        return default;
    }

    public int GetCategoryCount()
    {
        return Categories.Count;
    }

    public int GetCountOfState(State State)
    {
        int Count = 0;
        for (int i = 0; i < Categories.Count; i++)
        {
            foreach (var Tuple in Categories[i])
            {
                if (Tuple.Value != State)
                    continue;

                Count++;
            }
        }
        return Count;
    }

    public void AddCategory(SerializedDictionary<T, State> Category)
    {
        Categories.Add(Category);
    }

    public void AddCategory(T CategoryType, int MaxIndex)
    {
        SerializedDictionary<T, State> Category = new();
        int Mask = Service.GetValueAsInt(CategoryType);
        for (int i = 0; i <= MaxIndex; i++)
        {
            if ((Mask & (1 << i)) == 0)
                continue;

            Category.Add(Service.GetValueAsT(1 << i), State.Locked);
        }
        AddCategory(Category);
    }

    public State this[T Type]
    {
        get { return Get(Type); }
        set
        {
            Set(Type, value);
        }
    }

    public SerializedDictionary<T, State> GetCategory(int i)
    {
        return Categories[i];
    }

    public delegate void OnUnlock(T Type);
    public ActionList<T, State> _OnStateChanged = new();
    public ActionList<T> _OnTypeChanged = new();
}
