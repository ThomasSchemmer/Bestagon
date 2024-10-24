using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

/** 
 * Regulates unlocking of different things, should always be contained in a @IUnlockableService 
 * that handles the calling of methods / saving
 */
public abstract class Unlockables : ISaveableData
{
    public abstract byte[] GetData();
    public abstract int GetSize();
    public abstract void SetData(NativeArray<byte> Bytes);

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
    private List<SerializedDictionary<T, State>> Categories;

    public void Init(IUnlockableService<T> Service)
    {
        this.Service = Service;
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

    private bool HasCategoryAllUnlocked(int CategoryIndex)
    {
        foreach (var Tuple in Categories[CategoryIndex])
        {
            if (Tuple.Value == State.Locked)
                return false;
        }
        return true;
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

    public override byte[] GetData()
    {
        NativeArray<byte> Bytes = new(GetSize(), Allocator.Temp);
        int Pos = 0;

        // instead of having to create DTO's for every possible type and casting them into enums
        // we force conversion to int and save that
        for (int i = 0; i < Categories.Count; i++)
        {
            Pos = SaveGameManager.AddInt(Bytes, Pos, Categories[i].Count);
            foreach (var Tuple in Categories[i])
            {
                // unity does not allow (T)(IConvertible)Value casting, so create functions in the serviceee
                Pos = SaveGameManager.AddInt(Bytes, Pos, Service.GetValueAsInt(Tuple.Key));
                Pos = SaveGameManager.AddEnumAsByte(Bytes, Pos, (byte)Categories[i][Tuple.Key]);
            }
        }

        return Bytes.ToArray();
    }

    public override int GetSize()
    {
        return GetStaticSize(Categories.Count, GetCategoriesAmount());
    }

    public static int GetStaticSize(int CategoriesCount, int OverallCount)
    {
        // serialized categories with data and category size
        return sizeof(int) * CategoriesCount + (sizeof(int) + sizeof(byte)) * OverallCount;
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

    public override void SetData(NativeArray<byte> Bytes)
    {
        if (Service == null)
            return;

        Service.InitUnlockables();
        int Pos = 0;
        Categories.Clear();
        while (Pos < Bytes.Length)
        {
            SerializedDictionary<T, State> Category = new();
            Pos = SaveGameManager.GetInt(Bytes, Pos, out int CategorySize);
            // cast back into type with help of service
            for (int i = 0; i < CategorySize; i++)
            {
                Pos = SaveGameManager.GetInt(Bytes, Pos, out int Key);
                Pos = SaveGameManager.GetEnumAsByte(Bytes, Pos, out byte bValue);
                Category.Add(Service.GetValueAsT(Key), (State)bValue);

            }
            Categories.Add(Category);

            // now that the category has been added, we can execute callbacks
            for (int i = 0; i < Category.Count; i++)
            {
                T Key = Category.GetKeyAt(i);
                Service.OnLoadUnlockable(Key, Category[Key]);
            }
        }
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
