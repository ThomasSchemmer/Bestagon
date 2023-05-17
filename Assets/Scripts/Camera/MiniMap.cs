using System.Runtime.InteropServices;
using UnityEngine;

public class MiniMap : MonoBehaviour, UIElement
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

    public void ClickOn(Vector2 PixelPos) {
        RectTransform transform = GetComponent<RectTransform>();
        Rect Rectangle = new Rect(transform.anchoredPosition - transform.sizeDelta / 2.0f, transform.sizeDelta);
        if (!Rectangle.Contains(PixelPos))
            return;

        Vector2 PercentMiniMap;
        PercentMiniMap.x = (PixelPos.x - Rectangle.xMin) / Rectangle.width;
        PercentMiniMap.y = (PixelPos.y - Rectangle.yMin) / Rectangle.height;

        MapGenerator.GetMapBounds(out Vector3 MinBottomLeft, out Vector3 MaxTopRight);
        Vector3 WorldDiff = MaxTopRight - MinBottomLeft;
        Vector3 WorldPosition = MinBottomLeft + new Vector3(PercentMiniMap.x * WorldDiff.x, 0, PercentMiniMap.y * WorldDiff.z);
        CameraController.TargetPosition.x = WorldPosition.x;
        CameraController.TargetPosition.z = WorldPosition.z;
    }

    public void SetSelected(bool Selected) { }

    public void SetHovered(bool Hovered) {}

    public void Interact() {}

    public bool IsEqual(Selectable other) {
        return other is MiniMap;
    }


    public CustomRenderTexture MiniMapRT;
    private ComputeBuffer HexagonBuffer;
    private HexagonDTO[] DTOs;

    public static MiniMap Instance;
}
