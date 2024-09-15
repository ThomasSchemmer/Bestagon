using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/** Helper class to allow for casting different EntityVisualizations*/
public abstract class EntityVisualization : MonoBehaviour
{
}

/** Base class to create visualizations of any Entity.
 * Since its only a visualization, the logic is handled in the Entity class itself.
 * Displaying and updating the vis is done in @ChunkVisualization
 */
public abstract class EntityVisualization<T> : EntityVisualization where T: ScriptableEntity, ITokenized, IPreviewable
{
    public T Entity;

    public virtual void Init(T Entity)
    {
        this.Entity = Entity;
        Entity.SetVisualization(this);
    }

    public static EntityVisualization CreateFromData(T Entity) 
    {
        GameObject EntityObject = GetGameObjectFromEntity(Entity);
        EntityObject.transform.position = Entity.GetLocation().WorldLocation + Entity.GetOffset();
        EntityObject.transform.localRotation = Entity.GetRotation();

        EntityVisualization Vis = AddVisualization(EntityObject, Entity);
        return Vis;
    }


    public static EntityVisualization AddVisualization(GameObject Object, T Entity) 
    {
        EntityVisualization EntityVis = null;
        if (Entity is UnitEntity)
            EntityVis = Object.AddComponent<UnitVisualization>();
        if (Entity is BuildingEntity)
            EntityVis = Object.AddComponent<BuildingVisualization>();
        if (Entity is DecorationEntity)
            EntityVis = Object.AddComponent<DecorationVisualization>();

        EntityVisualization<T> EntityVisT = (EntityVisualization<T>)EntityVis;
        EntityVisT.Init(Entity);

        return EntityVis;
    }

    private static GameObject GetGameObjectFromEntity(T Entity)
    {
        if (!Game.TryGetService(out MeshFactory Factory))
            return null;

        switch (Entity.EntityType)
        {
            case ScriptableEntity.EType.Unit: return Factory.GetGameObjectFromType((Entity as UnitEntity).UnitType);
            case ScriptableEntity.EType.Building: return Factory.GetGameObjectFromType((Entity as BuildingEntity).BuildingType);
            case ScriptableEntity.EType.Decoration: return Factory.GetGameObjectFromType((Entity as DecorationEntity).DecorationType);
            default: return null;
        }

    }
}
