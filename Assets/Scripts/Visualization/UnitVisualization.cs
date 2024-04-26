using System.IO;
using UnityEngine;

public class UnitVisualization : MonoBehaviour
{
    public void UpdateLocation() {
        transform.position = Unit.Location.WorldLocation + Offset;
    }

    public void Refresh()
    {
        GameObject UnitPrefab = LoadPrefabFromFile(Unit.GetPrefabName());
        this.GetComponent<MeshFilter>().mesh = UnitPrefab.GetComponent<MeshFilter>().mesh;
    }

    public static UnitVisualization CreateFromData<T>(T Unit) where T : TokenizedUnitData
    {
        GameObject UnitObject = LoadPrefabFromFile(Unit.GetPrefabName());
        UnitObject.transform.position = Unit.Location.WorldLocation + Offset;

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

    private static GameObject LoadPrefabFromFile(string Name) {
        GameObject Prefab = Resources.Load("Units/" + Name) as GameObject;
        if (!Prefab) {
            throw new FileNotFoundException("Cannot load prefab for unit " + Name);
        }
        // only return a clone of the actual object, otherwise we will directly modify the original
        return Instantiate(Prefab);
    }

    public T GetUnitData<T>() where T : TokenizedUnitData
    {
        return (T)Unit;
    }

    public TokenizedUnitData Unit;

    public static Vector3 Offset = new Vector3(0, 6, 0);
}
