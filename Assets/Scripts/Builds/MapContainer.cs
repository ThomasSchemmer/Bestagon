using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;



[CreateAssetMenu(fileName = "MapData", menuName = "ScriptableObjects/MapData", order = 10)]
[Serializable]
public class MapContainer : ScriptableObject
{
    [SerializeField]
    public byte[] MapData;
}
