using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildingVisualization : MonoBehaviour
{
    public static BuildingVisualization CreateFromData<T>(T Building) where T : BuildingData
    {
        if (!Game.TryGetService(out MeshFactory Factory))
            return null;

        GameObject BuildingObject = Factory.GetGameObjectFromType(Building.BuildingType);
        BuildingObject.transform.position = Building.Location.WorldLocation + Building.GetOffset();
        BuildingObject.transform.localRotation = Building.GetRotation();
        BuildingVisualization Vis = AddVisualization(BuildingObject, Building);
        BuildingObject.AddComponent<WorkerIndicator>();
        return Vis;
    }

    public static BuildingVisualization AddVisualization<T>(GameObject Object, T Building) where T : BuildingData
    {
        BuildingVisualization BuildingVis = null;
        if (Building is BuildingData)
            BuildingVis = Object.AddComponent<BuildingVisualization>();

        BuildingVis.BuildingData = Building;

        return BuildingVis;
    }

    public BuildingData BuildingData;

}
