using System;
using Unity.Collections;
using UnityEngine;
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

    public UType UnitType;

    public virtual void Init(){
        EntityType = ScriptableEntity.EType.Unit;
    }

    public abstract bool TryInteractWith(HexagonVisualization Hex);

    /** This requires the UnitType to be saved as first position! */
    public static UnitEntity CreateSubFromSave(NativeArray<byte> Bytes, int Pos)
    {
        if (!Game.TryGetService(out MeshFactory MeshFactory))
            return null;

        // ignore first EntityType byte
        Pos = SaveGameManager.GetEnumAsByte(Bytes, Pos, out byte _);
        SaveGameManager.GetEnumAsByte(Bytes, Pos, out byte bUnitType);
        UType Type = (UType)bUnitType;

        return MeshFactory.CreateDataFromType(Type);
    }

}
