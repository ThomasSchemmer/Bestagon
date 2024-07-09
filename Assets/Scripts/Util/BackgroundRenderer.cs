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
        Vector3 MaxWorldLocation = HexagonConfig.TileSpaceToWorldSpace(MaxTileLocation);
        Vector3 OffsetWorldLocation = HexagonConfig.TileSpaceToWorldSpace(new(1, 1));
        Vector4 OffsetSize = new(
            OffsetWorldLocation.x * HexagonConfig.offsetX / HexagonConfig.TileSize.x * 0.75f,
            OffsetWorldLocation.z * HexagonConfig.offsetY / HexagonConfig.TileSize.z,
            0,
            0
        );
        Vector4 WorldSize = new Vector4(
            MaxWorldLocation.x,
            MaxWorldLocation.z,
            0,
            0
        );
        Material.SetVector("_WorldMax", WorldSize + OffsetSize);
    }
}
