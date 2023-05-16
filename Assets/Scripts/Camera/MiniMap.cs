using System.Runtime.InteropServices;
using UnityEngine;

public class MiniMap : MonoBehaviour
{
    public void Start() {
        Instance = this;
    }

    public void Init() {
        double start = Time.realtimeSinceStartupAsDouble;
        DTOs = MapGenerator.GetDTOs();
        HexagonBuffer = new ComputeBuffer(DTOs.Length, Marshal.SizeOf(DTOs[0]));
        HexagonBuffer.SetData(DTOs);
        MiniMapRT.material.SetBuffer("HexagonBuffer", HexagonBuffer);
        MiniMapRT.material.SetInt("_HexPerLine", HexagonConfig.mapMaxChunk * HexagonConfig.chunkSize);
        Debug.Log("Time diff: " + (Time.realtimeSinceStartupAsDouble - start));
    }

    public void FillBuffer() {
        DTOs = MapGenerator.GetDTOs();
        HexagonBuffer.SetData(DTOs);
    }

    public void Update() {
        if (DTOs == null) {
            Init();
        }

        MapGenerator.GetMapBounds(out Location BottomLeftMap, out Location TopRightMap);
        Vector4 BottomLeft = new Vector4(BottomLeftMap.GlobalTileLocation.x, BottomLeftMap.GlobalTileLocation.y);
        Vector4 TopRight = new Vector4(TopRightMap.GlobalTileLocation.x, TopRightMap.GlobalTileLocation.y);

        MiniMapRT.material.SetVector("_HexDistance", TopRight - BottomLeft);
        MiniMapRT.material.SetBuffer("HexagonBuffer", HexagonBuffer);
        MiniMapRT.material.SetInt("_HexPerLine", HexagonConfig.mapMaxChunk * HexagonConfig.chunkSize);

        Render();
    }

    private void Render() {
        MiniMapRT.Update();
    }

    public void OnDestroy() {
        HexagonBuffer.Release();
    }

    public CustomRenderTexture MiniMapRT;
    private ComputeBuffer HexagonBuffer;
    private HexagonDTO[] DTOs;

    public static MiniMap Instance;
}
