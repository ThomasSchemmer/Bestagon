using System;
using System.Collections.Generic;
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
    public List<GameObject> Prefabs = new();

    public virtual void Init(){
        EntityType = ScriptableEntity.EType.Unit;
    }

    public abstract bool TryInteractWith(HexagonVisualization Hex);
    public abstract int GetTargetMeshIndex();

    public new static int CreateFromSave(NativeArray<byte> Bytes, int Pos, out ScriptableEntity Unit)
    {
        Unit = null;
        if (!Game.TryGetService(out MeshFactory MeshFactory))
            return -1;

        // ignore first EntityType byte
        Pos = SaveGameManager.GetEnumAsByte(Bytes, Pos, out byte _);
        Pos = SaveGameManager.GetEnumAsByte(Bytes, Pos, out byte bUnitType);
        UType Type = (UType)bUnitType;

        Unit = MeshFactory.CreateDataFromType(Type);
        return Pos;
    }

}
