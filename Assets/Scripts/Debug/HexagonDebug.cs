using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexagonDebug : MonoBehaviour, Debuggable
{
    public string GetDebugString() {
        return this.name;
    }
}
