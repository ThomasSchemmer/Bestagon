using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class BuildingVisualization : MonoBehaviour
{
    public static BuildingVisualization CreateFromData(BuildingData InData) {
        if (!Game.TryGetService(out MeshFactory TileFactory))
            return null;

        GameObject BuildingObject = TileFactory.GetBuildingFromType(InData.BuildingType);
        BuildingObject.transform.position = InData.GetOffset() + InData.Location.WorldLocation;
        BuildingObject.transform.localRotation = InData.GetRotation();

        BuildingVisualization BuildingVis = BuildingObject.AddComponent<BuildingVisualization>();

        return BuildingVis;
    }

}
