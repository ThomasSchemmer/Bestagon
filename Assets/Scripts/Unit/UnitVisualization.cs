using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Assertions;

public class UnitVisualization : MonoBehaviour
{
    public void UpdateLocation() {
        transform.position = Unit.Location.WorldLocation + Offset;
    }

    public static UnitVisualization CreateFromData<T>(T Unit) where T : UnitData {
        GameObject UnitObject = LoadPrefabFromFile(Unit.GetPrefabName());
        UnitObject.transform.position = Unit.Location.WorldLocation + Offset;

        UnitVisualization UnitVis = UnitObject.AddComponent<UnitVisualization>();
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

    public T GetUnitData<T>() where T : UnitData
    {
        return (T)Unit;
    }

    private UnitData Unit;

    public static Vector3 Offset = new Vector3(0, 6, 0);
}
