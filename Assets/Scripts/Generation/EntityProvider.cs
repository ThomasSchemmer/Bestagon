using Microsoft.Win32;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

/** Base class providing access for any one type of entity.
 * Useful for shared saving/loading
 */
// todo: save spatially effient, either chunks or quadtree etc
public class EntityProvider<T> : GameService, IQuestRegister<T>, ISaveableService where T : ScriptableEntity
{
    public List<T> Entities = new();

    public void RefreshEntities()
    {
        foreach (T ActiveEntity in Entities)
        {
            ActiveEntity.Refresh();
        }
    }

    public virtual void KillEntity(T Entity)
    {
        Entities.Remove(Entity);
    }

    public void KillAllEntities()
    {
        int Count = Entities.Count;
        for (int i = Count - 1; i >= 0; i--)
        {
            KillEntity(Entities[i]);
        }
    }

    public virtual int GetSize()
    {
        // unit count + overall size
        return GetEntitiesSize() + sizeof(int) * 2;
    }

    private int GetEntitiesSize()
    {
        int Size = 0;
        foreach (T Entity in Entities)
        {
            Size += Entity.GetSize();
        }
        return Size;
    }

    public virtual byte[] GetData()
    {
        // will be called for eg Units, but will be overwritten in BuildingService
        NativeArray<byte> Bytes = new(GetSize(), Allocator.Temp);
        return GetData(Bytes);
    }

    /** Since BuildingService overrides GeetSize and there is no static size, we have to use a fixed size array here!*/
    public byte[] GetData(NativeArray<byte> Bytes)
    {
        int Pos = 0;
        // save the size to make reading it easier
        Pos = SaveGameManager.AddInt(Bytes, Pos, GetSize());
        Pos = SaveGameManager.AddInt(Bytes, Pos, Entities.Count);

        foreach (T Entity in Entities)
        {
            Pos = SaveGameManager.AddSaveable(Bytes, Pos, Entity);
        }

        return Bytes.ToArray();
    }

    public virtual void SetData(NativeArray<byte> Bytes)
    {
        // skip overall size info at the beginning
        int Pos = sizeof(int);
        Pos = SaveGameManager.GetInt(Bytes, Pos, out int EntitiesLength);

        Entities = new();
        for (int i = 0; i < EntitiesLength; i++)
        {
            ScriptableEntity.CreateFromSave(Bytes, Pos, out ScriptableEntity Entity);
            if (Entity is not T)
            {
                throw new System.Exception("Cannot create entity from data entry");
            }

            Pos = SaveGameManager.SetSaveable(Bytes, Pos, Entity);
            Entities.Add(Entity as T);
        }
    }

    public virtual void Reset()
    {
        KillAllEntities();
        Entities = new();
    }

    public bool TryGetAnyOfType(ScriptableEntity.EType Type, out T Unit)
    {
        Unit = default;
        foreach (T Temp in Entities)
        {
            if (Temp.EntityType != Type)
                continue;

            Unit = Temp;
            return true;
        }
        return false;
    }

    public bool TryGetAnyOfType(UnitEntity.UType Type, out UnitEntity Unit)
    {
        Unit = default;
        foreach (T Temp in Entities)
        {
            if (Temp.EntityType != ScriptableEntity.EType.Unit)
                continue;

            Unit = Temp as UnitEntity;
            if (Unit == null)
                continue;

            if (Unit.UnitType != Type)
                continue;

            return true;
        }
        return false;
    }

    public bool TryGetEntityToBeMalaised(out T Entity)
    {
        Entity = default;
        foreach (var FoundEntity in Entities)
        {
            if (!FoundEntity.IsAboutToBeMalaised())
                continue;

            Entity = FoundEntity;
            return true;
        }

        return false;
    }

    public bool ShouldLoadWithLoadedSize() { return true; }

    protected override void StartServiceInternal() { }
    protected override void StopServiceInternal() { }

    public virtual void OnLoaded() { }

    public static ActionList<T> _OnEntityCreated = new();
}
