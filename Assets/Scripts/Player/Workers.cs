using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Workers : MonoBehaviour
{
    public void Start() {
        Instance = this;

        for (int i = 0; i < MaxWorker; i++) {
            UnassignedWorker.Add(new Worker() {
                Name = "Worker " + i
            });
        }    
    }

    public static Worker GetWorker() {
        if (!Instance)
            return null;

        if (Instance.UnassignedWorker.Count == 0)
            return null;

        Worker Worker = Instance.UnassignedWorker[0];
        Instance.UnassignedWorker.RemoveAt(0);
        return Worker;
    }

    public static void ReturnWorker(Worker Worker) {
        if (!Instance || Worker == null)
            return;

        Instance.UnassignedWorker.Add(Worker);
    }

    internal static int GetNrOfWorkingWorker() {
        if (!Instance)
            return 0;

        return MaxWorker - Instance.UnassignedWorker.Count;
    }

    internal static int GetNrOfIdleWorker() {
        if (!Instance)
            return 0;

        return Instance.UnassignedWorker.Count;
    }

    public List<Worker> UnassignedWorker = new List<Worker>();

    public static Workers Instance;
    public static int MaxWorker = 10;
}
