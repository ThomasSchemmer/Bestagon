using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

/** 
 * Base class for anything providing access to a certain type of units
 * Mainly useful for saving and loading the data
 */
public class UnitProvider<T> : GameService, IQuestRegister<UnitData>, ISaveableService where T : UnitData
{
    public List<T> Units = new();

    public void RefreshUnits()
    {
        foreach (T ActiveUnit in Units)
        {
            ActiveUnit.Refresh();
        }
    }

    public virtual void KillUnit(T Unit)
    {
        Units.Remove(Unit);
    }

    public void KillAllUnits()
    {
        int Count = Units.Count;
        for (int i = Count - 1; i >= 0; i--)
        {
            KillUnit(Units[i]);
        }
    }

    public int GetSize()
    {
        // unit count + overall size
        return GetUnitsSize() + sizeof(int) * 2;
    }

    private int GetUnitsSize()
    {
        int Size = 0;
        foreach (T Unit in Units)
        {
            Size += Unit.GetSize();
        }
        return Size;
    }

    public byte[] GetData()
    {
        NativeArray<byte> Bytes = new(GetSize(), Allocator.Temp);
        int Pos = 0;
        // save the size to make reading it easier
        Pos = SaveGameManager.AddInt(Bytes, Pos, GetSize());
        Pos = SaveGameManager.AddInt(Bytes, Pos, Units.Count);

        foreach (T Unit in Units)
        {
            Pos = SaveGameManager.AddSaveable(Bytes, Pos, Unit);
        }

        return Bytes.ToArray();
    }

    public void SetData(NativeArray<byte> Bytes)
    {
        // skip overall size info at the beginning
        int Pos = sizeof(int);
        Pos = SaveGameManager.GetInt(Bytes, Pos, out int UnitsLength);

        Units = new();
        for (int i = 0; i < UnitsLength; i++)
        {
            UnitData Unit = UnitData.CreateSubFromSave(Bytes, Pos);
            if (Unit is not T)
                continue;

            Pos = SaveGameManager.SetSaveable(Bytes, Pos, Unit);
            Units.Add(Unit as T);
        }
    }

    public void Reset()
    {
        Units = new();
    }

    public bool ShouldLoadWithLoadedSize() { return true; }

    protected override void StartServiceInternal() { }
    protected override void StopServiceInternal() { }

    public static ActionList<UnitData> _OnUnitCreated = new();
}
