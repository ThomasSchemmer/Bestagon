using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class BuildingVisualization : MonoBehaviour
{
    public static BuildingVisualization CreateFromData(BuildingData InData) {
        GameObject BuildingObject = LoadPrefabFromFile(InData.BuildingType.ToString());
        BuildingObject.transform.position = InData.GetOffset() + InData.Location.WorldLocation;
        BuildingObject.transform.localRotation = InData.GetRotation();

        BuildingVisualization BuildingVis = BuildingObject.AddComponent<BuildingVisualization>();
        BuildingVis.Data = InData;

        return BuildingVis;
    }

    private static GameObject LoadPrefabFromFile(string Name) {
        GameObject Prefab = Resources.Load("Buildings/" + Name) as GameObject;
        if (!Prefab) {
            throw new FileNotFoundException("Cannot load building for card " + Name);
        }
        // only return a clone of the actual object, otherwise we will directly modify the original
        return Instantiate(Prefab);
    }

    BuildingData Data;
}
