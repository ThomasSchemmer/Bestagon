using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class Debugger : MonoBehaviour
{
    private void OnGUI() {
        if (!Application.isEditor)
            return;

        GameObject Selected = Selection.gameObjects.Length > 0 ? Selection.gameObjects[0] : null;
        if (!Selected)
            return;

        Debuggable debuggable = Selected.GetComponent<Debuggable>();
        if (debuggable == null)
            return;

        GUI.Label(new Rect(0, 0, 200, 100), debuggable.GetDebugString());
    }
}
