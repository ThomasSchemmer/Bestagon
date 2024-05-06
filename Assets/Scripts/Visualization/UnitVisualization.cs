using System.IO;
using UnityEngine;

public class UnitVisualization : MonoBehaviour
{
    public void UpdateLocation() {
        transform.position = Unit.Location.WorldLocation + Unit.GetOffset();
    }

    public void Refresh()
    {
        if (!Game.TryGetService(out MeshFactory Factory))
            return;

        GameObject UnitPrefab = Factory.GetGameObjectFromType(Unit.Type);
        this.GetComponent<MeshFilter>().mesh = UnitPrefab.GetComponent<MeshFilter>().mesh;
    }

    public static UnitVisualization CreateFromData<T>(T Unit) where T : TokenizedUnitData
    {
        if (!Game.TryGetService(out MeshFactory Factory))
            return null;

        GameObject UnitObject = Factory.GetGameObjectFromType(Unit.Type);
        UnitObject.transform.position = Unit.Location.WorldLocation + Unit.GetOffset();
        UnitObject.transform.localRotation = Unit.GetRotation();

        return AddVisualization(UnitObject, Unit);
    }

    public static UnitVisualization AddVisualization<T>(GameObject Object, T Unit) where T : TokenizedUnitData
    {
        UnitVisualization UnitVis = null;
        if (Unit is UnitData)
            UnitVis = Object.AddComponent<UnitVisualization>();

        UnitVis.Unit = Unit;
        Unit.Visualization = UnitVis;

        return UnitVis;
    }

    public T GetUnitData<T>() where T : TokenizedUnitData
    {
        return (T)Unit;
    }

    public TokenizedUnitData Unit;
}
