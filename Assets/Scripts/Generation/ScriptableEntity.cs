using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

/** 
 * Base class for any game entity that is not a hex, see @UnitData, @DecorationData 
 * Does not need to be visualized, see @WorkerData
 * Is loaded on runtime from a SO definition
 */
public abstract class ScriptableEntity : ScriptableObject
{
    public enum EType
    {
        Unit,
        Building,
        Decoration
    }

    [SaveableEnum]
    public EType EntityType;

    public virtual void Refresh() { }

    public abstract bool IsAboutToBeMalaised();
    public abstract bool IsIdle();
}
