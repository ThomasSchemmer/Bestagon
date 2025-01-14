using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/** Provides functionality to query entities based on their location */
public abstract class TokenizedEntityProvider<T> : EntityProvider<T> where T : ScriptableEntity, ITokenized
{
    public bool TryGetEntityAt(Location Location, out T Entity)
    {
        Entity = null;
        foreach (T ActiveEntity in Entities)
        {
            if (!ActiveEntity.GetLocations().Contains(Location))
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

    public bool IsEntityAt(LocationSet Locations)
    {
        return Locations.Any(_ => IsEntityAt(_));
    }

    public bool TryGetEntitiesInChunk(Location ChunkLocation, out List<T> EntitiesInChunk)
    {
        EntitiesInChunk = new();
        foreach (T Entity in Entities)
        {
            if (Entity.GetLocations().Count(Location => Location.ChunkLocation.Equals(ChunkLocation.ChunkLocation)) == 0)
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

    public override void OnAfterLoaded()
    {
        base.OnAfterLoaded();
        if (!Game.TryGetService(out MapGenerator MapGenerator))
            return;

        HashSet<ChunkVisualization> ChunksToRefresh = new();
        foreach (var Entity in Entities)
        {
            if (!MapGenerator.TryGetChunkVis(Entity.GetLocations(), out var Chunks))
                continue;

            foreach (var Chunk in Chunks)
            {
                ChunksToRefresh.Add(Chunk);
            }
        }
        foreach(var Chunk in ChunksToRefresh)
        {
            Chunk.RefreshTokens();
        }
    }

}
