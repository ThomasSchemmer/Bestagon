using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

/** 
 * Base class for any game entity that is not a hex, see @UnitData, @DecorationData 
 * Does not need to be visualized, see @WorkerData
 * Is loaded on runtime from a SO definition
 */
public abstract class ScriptableEntity : ScriptableObject, ISaveableData
{
    public enum EType
    {
        Unit,
        Building,
        Decoration
    }

    public EType EntityType;

    public virtual void Refresh() { }

    public abstract int GetSize();

    public abstract byte[] GetData();

    public abstract void SetData(NativeArray<byte> Bytes);

    public static int CreateFromSave(NativeArray<byte> Bytes, int Pos, out ScriptableEntity Entity)
    {
        Entity = null;
        return -1;
    }
}
