using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using static UnityEngine.UI.CanvasScaler;
/** 
 * Base class for any unit. Is extended (with middle classes) for worker and scouts 
 * Any unit class only contains data, but does not have to be visualized
 */
[Serializable]
public abstract class UnitEntity : ScriptableEntity
{
    public enum UType
    {
        Worker,
        Scout
    }

    [SaveableEnum]
    public UType UnitType;
    // only filled in the actual definition, not instances
    public List<GameObject> Prefabs = new();

    public virtual void Init(){
        EntityType = ScriptableEntity.EType.Unit;
    }

    public abstract bool TryInteractWith(HexagonVisualization Hex);
    public abstract int GetTargetMeshIndex();

}
