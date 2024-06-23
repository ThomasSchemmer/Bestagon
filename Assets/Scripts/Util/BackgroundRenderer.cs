using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackgroundRenderer : MonoBehaviour
{
    public Material Material;

    void Start()
    {
        Location MaxLocation = HexagonConfig.GetMaxLocation();
        Vector2Int MaxTileLocation = MaxLocation.GlobalTileLocation;
        Vector4 WorldSize = new Vector4(
            MaxTileLocation.x * HexagonConfig.TileSize.x,
            MaxTileLocation.y * HexagonConfig.TileSize.z,
            0,
            0
        );
        Material.SetVector("_WorldMax", WorldSize);
    }
}
