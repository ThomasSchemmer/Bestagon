using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Assertions;

public class WorkerVisualization : MonoBehaviour
{
    public void UpdateLocation() {
        Assert.IsNotNull(Worker);
        transform.position = Worker.Location.WorldLocation + Offset;
    }

    public static WorkerVisualization CreateFromData(WorkerData InWorker) {
        GameObject WorkerObject = LoadPrefabFromFile("Worker");
        WorkerObject.transform.position = InWorker.Location.WorldLocation + Offset;

        WorkerVisualization WorkerVis = WorkerObject.AddComponent<WorkerVisualization>();
        WorkerVis.Worker = InWorker;
        InWorker.Visualization = WorkerVis;

        return WorkerVis;
    }

    private static GameObject LoadPrefabFromFile(string Name) {
        GameObject Prefab = Resources.Load("Units/" + Name) as GameObject;
        if (!Prefab) {
            throw new FileNotFoundException("Cannot load prefab for unit " + Name);
        }
        // only return a clone of the actual object, otherwise we will directly modify the original
        return Instantiate(Prefab);
    }

    WorkerData Worker;

    public static Vector3 Offset = new Vector3(0, 6, 0);
}
