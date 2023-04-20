using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

/**
 * Visualization for a specific chunk
 * Information of what to visualize gets passed in via ChunkData and later Buildings
 * Contains a reference to all its hexes, which will be changed/regenerated on runtime
 */
public class ChunkVisualization : MonoBehaviour
{
    public IEnumerator GenerateMeshesAsync(ChunkData Data, Material HexMat, Material MalaiseMat, List<Mesh> Meshes) 
    {
        this.name = "Chunk " + Data.Location.ChunkLocation;
        this.Data.Visualization = null;
        this.Data = Data;
        Data.Visualization = this;

        for (int y = 0; y < HexagonConfig.chunkSize; y++) {
            for (int x = 0; x < HexagonConfig.chunkSize; x++) {
                // simply add the base position of the chunk as the bottom left corner
                Location Location = Location.CreateHex(x, y) + Data.Location;
                Hexes[x, y].Init(Data, Location, HexMat, Meshes);
            }
            yield return null;
        }

        MalaiseVisualization.Initialize(Data.Malaise, MalaiseMat);
        CreateBuildings();
    }

    public void Initialize() {
        Hexes = new HexagonVisualization[HexagonConfig.chunkSize, HexagonConfig.chunkSize];

        for (int y = 0; y < HexagonConfig.chunkSize; y++) {
            for (int x = 0; x < HexagonConfig.chunkSize; x++) {
                GameObject HexObj = new GameObject();
                HexObj.transform.parent = this.transform;
                Hexes[x, y] = HexObj.AddComponent<HexagonVisualization>();
            }
        }

        GameObject MalaiseObj = new GameObject();
        MalaiseObj.transform.parent = this.transform;
        MalaiseVisualization = MalaiseObj.AddComponent<MalaiseVisualization>();

        BuildingVisualizations = new();
    }

    public void CreateBuildings() {
        BuildingVisualizations = new();
        foreach (BuildingData BuildingData in Data.Buildings) {
            BuildingVisualization Vis = BuildingVisualization.CreateFromData(BuildingData);
            Vis.transform.parent = transform;
            BuildingVisualizations.Add(Vis);
        }
    }

    public void CreateBuilding(BuildingData BuildingData) {
        BuildingVisualization Vis = BuildingVisualization.CreateFromData(BuildingData);
        Vis.transform.parent = transform;
        BuildingVisualizations.Add(Vis);
    }

    public void Reset() {
        foreach (BuildingVisualization Building in BuildingVisualizations) {
            Destroy(Building.gameObject);
        }
        BuildingVisualizations.Clear();
    }

    public HexagonVisualization[,] Hexes;
    public List<BuildingVisualization> BuildingVisualizations;
    public MalaiseVisualization MalaiseVisualization;
    public ChunkData Data;
    public Coroutine Generator;
}
