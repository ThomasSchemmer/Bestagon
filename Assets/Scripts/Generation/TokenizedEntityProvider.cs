using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/** Provides functionality to query entities based on their location */
public class TokenizedEntityProvider<T> : EntityProvider<T> where T : ScriptableEntity, ITokenized
{
    public bool TryGetEntityAt(Location Location, out T Entity)
    {
        Entity = null;
        foreach (T ActiveEntity in Entities)
        {
            if (!ActiveEntity.GetLocation().Equals(Location))
                continue;

            Entity = ActiveEntity;
            return true;
        }

        return false;
    }

    public bool IsEntityAt(Location Location)
    {
        return TryGetEntityAt(Location, out T _);
    }

    public bool TryGetEntitiesInChunk(Location ChunkLocation, out List<T> EntitiesInChunk)
    {
        EntitiesInChunk = new();
        foreach (T Entity in Entities)
        {
            if (!Entity.GetLocation().ChunkLocation.Equals(ChunkLocation.ChunkLocation))
                continue;

            EntitiesInChunk.Add(Entity);
        }

        return EntitiesInChunk.Count > 0;
    }

    public bool HasAnyEntity(ScriptableEntity.EType EntityType, out T FoundEntity)
    {
        foreach (T Entity in Entities)
        {
            if (Entity.EntityType != EntityType)
                continue;

            FoundEntity = Entity;
            return true;
        }
        FoundEntity = default;
        return false;
    }

}
