using Unity.Collections;
using UnityEngine;

/** 
 * Base class for any unit. Is extended (with middle classes) for worker and scouts 
 * Any unit class only contains data.
 */
public abstract class UnitData : ScriptableObject, ISaveableData
{
    public enum UnitType
    {
        Worker,
        Scout
    }

    public UnitType Type;

    public virtual void Init()
    {
        _OnUnitCreated?.Invoke(this);
    }


    public virtual void Refresh() {}

    public abstract int GetSize();

    public abstract byte[] GetData();

    public abstract void SetData(NativeArray<byte> Bytes);

    public abstract bool TryInteractWith(HexagonVisualization Hex);

    public static UnitData CreateSubFromSave(NativeArray<byte> Bytes, int Pos)
    {
        if (!Game.TryGetService(out MeshFactory MeshFactory))
            return null;

        SaveGameManager.GetEnumAsByte(Bytes, Pos, out byte bType);
        UnitType Type = (UnitType)bType;

        return MeshFactory.CreateDataFromType(Type);
    }

    public delegate void OnUnitCreated(UnitData Unit);
    public static event OnUnitCreated _OnUnitCreated;
}
