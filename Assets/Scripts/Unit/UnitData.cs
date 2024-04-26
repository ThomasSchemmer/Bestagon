using Unity.Collections;

/** 
 * Base class for any unit. Is extended (with middle classes) for worker and scouts 
 * Any unit class only contains data.
 */
public abstract class UnitData : ISaveable
{
    public enum UnitType
    {
        Worker,
        Scout
    }

    public UnitType Type;

    public virtual void Refresh() {}

    public abstract int GetSize();

    public abstract byte[] GetData();

    public abstract void SetData(NativeArray<byte> Bytes);
}
