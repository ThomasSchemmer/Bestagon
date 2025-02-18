using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/** 
 * Provides functionality to query entities based on their location 
 * TODO: make spatially efficient!
 */
public abstract class TokenizedEntityProvider<T> : EntityProvider<T> where T : ScriptableEntity, ITokenized
{
    public bool TryGetEntityAt(Location Location, out T Entity, int Range = 0, int Type = -1)
    {
        Entity = null;
        var Params = new Pathfinding.Parameters(true, false, false);
        HashSet<Location> ReachableLocations = Pathfinding.FindReachableLocationsFrom(Location, Range, Params);
        foreach (T ActiveEntity in Entities)
        {
            if (Type != -1 && Type != (int)ActiveEntity.GetUType())
                continue;

            if (!ActiveEntity.GetLocations().ContainsAny(ReachableLocations))
                continue;

            Entity = ActiveEntity;
            return true;
        }

        return false;
    }

    public bool IsEntityAt(Location Location, int Range = 0, int Type = -1)
    {
        return TryGetEntityAt(Location, out T _, Range, Type);
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

    public override void RemoveEntity(T Entity)
    {
        LocationSet Location = Entity.GetLocations();
        base.RemoveEntity(Entity);

        if (!Game.TryGetService(out MapGenerator MapGen))
            return;

        if (!MapGen.TryGetChunkVis(Location, out var Chunks))
            return;

        Chunks.ForEach(Chunk => Chunk.RefreshTokens());
    }

}
