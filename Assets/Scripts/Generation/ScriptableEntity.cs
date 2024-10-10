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

    public virtual int GetSize()
    {
        return GetStaticSize();
    }

    public static int GetStaticSize()
    {
        // entity type
        return sizeof(byte);
    }

    public virtual byte[] GetData()
    {
        NativeArray<byte> Bytes = new(GetStaticSize(), Allocator.Temp);

        int Pos = 0;
        Pos = SaveGameManager.AddEnumAsByte(Bytes, Pos, (byte)EntityType);
        return Bytes.ToArray();
    }

    public virtual void SetData(NativeArray<byte> Bytes)
    {
        int Pos = 0;
        Pos = SaveGameManager.GetEnumAsByte(Bytes, Pos, out byte bEntityType);
        EntityType = (EType)bEntityType;
    }

    public static int CreateFromSave(NativeArray<byte> Bytes, int Pos, out ScriptableEntity Entity)
    {
        Entity = default;

        SaveGameManager.GetEnumAsByte(Bytes, Pos, out byte bEntityType);
        EType EntityType = (EType)bEntityType;
        switch (EntityType)
        {
            case EType.Unit: return UnitEntity.CreateFromSave(Bytes, Pos, out Entity);
            case EType.Building: return BuildingEntity.CreateFromSave(Bytes, Pos, out Entity);
            case EType.Decoration: return DecorationEntity.CreateFromSave(Bytes, Pos, out Entity);
            default: return -1;
        }
    }
}
