using System.Drawing.Drawing2D;
using System.Drawing;
using System.Runtime.InteropServices;
using UnityEngine;
using System.Linq.Expressions;

public class MiniMap : GameService, UIElement
{
    public void Init() {
        
        Game.RunAfterServiceInit((MapGenerator Generator) =>
        {
            MapGenerator = Generator;
            DTOs = Generator.GetDTOs();
            ClearBuffer();
            HexagonBuffer = new ComputeBuffer(DTOs.Length, Marshal.SizeOf(DTOs[0]));
            HexagonBuffer.SetData(DTOs);
            MiniMapRT.material.SetBuffer("HexagonBuffer", HexagonBuffer);
            MiniMapRT.material.SetInt("_HexPerLine", HexagonConfig.MapMaxChunk * HexagonConfig.ChunkSize);
            _OnInit?.Invoke(this);
            FillBuffer();
        });
    }

    public bool CanBeLongHovered()
    {
        return true;
    }

    public string GetHoverTooltip()
    {
        return "The MiniMap shows an overview over the explored world as well as information about the current viewing position";
    }

    public void FillBuffer() {
        if (!IsInit)
            return;

        DTOs = MapGenerator.GetDTOs();
        HexagonBuffer.SetData(DTOs);
    }

    public void Update() {
        if (!IsRunning || !IsInit)
            return;

        MapGenerator.GetMapBounds(out Location BottomLeftMap, out Location TopRightMap);
        Vector4 BottomLeft = new Vector4(BottomLeftMap.GlobalTileLocation.x, BottomLeftMap.GlobalTileLocation.y);
        Vector4 TopRight = new Vector4(TopRightMap.GlobalTileLocation.x, TopRightMap.GlobalTileLocation.y);

        MiniMapRT.material.SetVector("_HexDistance", TopRight - BottomLeft);
        MiniMapRT.material.SetBuffer("HexagonBuffer", HexagonBuffer);
        MiniMapRT.material.SetInt("_HexPerLine", HexagonConfig.MapMaxChunk * HexagonConfig.ChunkSize);

        Render();
    }

    private void Render() {
        MiniMapRT.Update();
    }

    public void OnDestroy()
    {
        ClearBuffer();
    }

    private void ClearBuffer()
    {
        if (HexagonBuffer != null)
        {
            HexagonBuffer.Release();
        }
    }

    public void ClickOn(Vector2 PixelPos) {
        if (!IsInit)
            return;

        RectTransform Transform = GetComponent<RectTransform>();
        Size Size = new((int)Transform.sizeDelta.x, (int)Transform.sizeDelta.y);
        Point Center = new((int)Transform.position.x, (int)Transform.position.y);
        Rectangle Rectangle = new(Center - Size / 2, Size);

        Point P = new Point((int)PixelPos.x, (int)PixelPos.y);
        P = Rotate(Center, P, -Transform.eulerAngles.z);

        if (!Rectangle.Contains(P))
            return;

        Vector2 PercentMiniMap;
        PercentMiniMap.x = (P.X - Rectangle.Left) / (float)Rectangle.Width;
        PercentMiniMap.y = (P.Y - Rectangle.Top) / (float)Rectangle.Height;

        MapGenerator.GetMapBounds(out Vector3 MinBottomLeft, out Vector3 MaxTopRight);
        Vector3 WorldDiff = MaxTopRight - MinBottomLeft;
        Vector3 WorldPosition = MinBottomLeft + new Vector3(PercentMiniMap.x * WorldDiff.x, 0, PercentMiniMap.y * WorldDiff.z);
        CameraController.TargetPosition.x = WorldPosition.x;
        CameraController.TargetPosition.z = WorldPosition.z;
    }

    private Point Rotate(Point Center, Point P, float Angle)
    {
        Matrix M = new Matrix();
        M.RotateAt(Angle, Center);
        Point[] Ps = new Point[] { P };
        M.TransformPoints(Ps);
        return Ps[0];
    }

    public void SetSelected(bool Selected) { }

    public void SetHovered(bool Hovered) {}

    public void Interact() {}

    public bool IsEqual(ISelectable other) {
        return other is MiniMap;
    }

    public void PassData(Vector4 TopView, Vector4 BottomView)
    {
        MiniMapRT.material.SetVector("_TopView", TopView);
        MiniMapRT.material.SetVector("_BottomView", BottomView);
    }

    protected override void StartServiceInternal()
    {
        Init();
    }

    protected override void StopServiceInternal()
    {
        gameObject.SetActive(false);
        IsInit = false;
    }

    public CustomRenderTexture MiniMapRT;
    private ComputeBuffer HexagonBuffer;
    private HexagonDTO[] DTOs;
    private MapGenerator MapGenerator;
}
