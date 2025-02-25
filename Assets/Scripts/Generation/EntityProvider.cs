using Microsoft.Win32;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

/** Base class providing access for any one type of entity.
 * Useful for shared saving/loading
 */
// todo: save spatially effient, either chunks or quadtree etc
public abstract class EntityProvider : SaveableService
{
    protected override void StartServiceInternal() { }
    protected override void StopServiceInternal() { }

    // has to be int as all flags are 32 bit
    public abstract bool TryCreateNewEntity(int EntityCode, LocationSet Location);
    public abstract int GetAmountOfType(int EntityCode);
}

public abstract class EntityProvider<T> : EntityProvider, IQuestRegister<T> where T : ScriptableEntity
{

    [SaveableList]
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
        RemoveEntity(Entity);
        Destroy(Entity);
    }

    public virtual void RemoveEntity(T Entity)
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

    protected override void ResetInternal()
    {
        KillAllEntities();
        Entities = new();
    }

    public GameObject GetGameObject() { return gameObject; }

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

    public bool TryGetAnyOfType(BuildingConfig.Type Type, out BuildingEntity Building)
    {
        Building = default;
        if (!TryGetAllOfType(Type, out var List))
            return false;

        Building = List.Count > 0 ? List[0] : default;
        return Building != null;
    }

    public bool TryGetAllOfType(BuildingConfig.Type Type, out List<BuildingEntity> Buildings)
    {
        Buildings = new();
        foreach (T Temp in Entities)
        {
            if (Temp.EntityType != ScriptableEntity.EType.Building)
                continue;

            var Building = Temp as BuildingEntity;
            if (Building == null)
                continue;

            if (Building.BuildingType != Type)
                continue;

            Buildings.Add(Building);
        }
        return Buildings.Count > 0;
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

    public virtual bool TryGetIdleEntity(out T Entity)
    {
        Entity = default;
        foreach (var FoundEntity in Entities)
        {
            if (!FoundEntity.IsIdle())
                continue;

            Entity = FoundEntity; 
            return true;
        }
        return false;
    }

    public static ActionList<T> _OnEntityCreated = new();
}
