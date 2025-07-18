﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : GameService, IQuestRegister<Vector3>
{
    protected override void StartServiceInternal()
    {
        Cam = GetComponent<Camera>();
        OverlayCam = transform.GetChild(2).GetComponent<Camera>();
        TargetPosition = this.transform.position;
        Game.Instance._OnPause += OnPause;
        Game.Instance._OnResume += OnResume;
        Game.RunAfterServiceInit((MapGenerator MapGen) =>
        {
            _OnInit?.Invoke(this);
        });
    }

    protected override void StopServiceInternal() {}

    void FixedUpdate() {
        if (IsPaused || !IsInit)
            return;

        UpdatePosition();
        MoveToPosition();
        UpdateMiniMapData();
    }

    private void OnPause()
    {
        IsPaused = true;
    }

    private void OnResume()
    {
        IsPaused = false;
    }

    private void UpdateMiniMapData() {
        if (!Game.TryGetService(out MapGenerator MapGenerator))
            return;

        // takes the 4 view frustrum corners and translates them into uv space 
        // according to world map position
        Plane Plane = new Plane(Vector3.up, Vector3.zero);

        Ray TopLeftRay = Cam.ScreenPointToRay(new Vector3(0, Screen.height, 0));
        Ray TopRightRay = Cam.ScreenPointToRay(new Vector3(Screen.width, Screen.height, 0));
        Ray BottomRightRay = Cam.ScreenPointToRay(new Vector3(Screen.width, 0, 0));
        Ray BottomLeftRay = Cam.ScreenPointToRay(new Vector3(0, 0, 0));

        Plane.Raycast(TopLeftRay, out float TopLeftScale);
        Plane.Raycast(TopRightRay, out float TopRightScale);
        Plane.Raycast(BottomRightRay, out float BottomRightScale);
        Plane.Raycast(BottomLeftRay, out float BottomLeftScale);

        MapGenerator.GetMapBounds(out Vector3 MinBottomLeftWorld, out Vector3 MaxTopRightWorld);
        Vector3 DistanceMap = MaxTopRightWorld - MinBottomLeftWorld;

        Vector3 TopLeftPos = TopLeftRay.GetPoint(TopLeftScale);
        Vector3 TopRightPos = TopRightRay.GetPoint(TopRightScale);
        Vector3 BottomRightPos = BottomRightRay.GetPoint(BottomRightScale);
        Vector3 BottomLeftPos = BottomLeftRay.GetPoint(BottomLeftScale);

        // each vector4 contains two uv positions at each (x, y) and (z, w)
        // the pos vectors for the frustum edges are still in (x, y, z) coordinates!
        Vector4 TopView = new Vector4();
        Vector4 BottomView = new Vector4();
        TopView.x = (TopLeftPos.x - MinBottomLeftWorld.x) / DistanceMap.x;
        TopView.y = (TopLeftPos.z - MinBottomLeftWorld.z) / DistanceMap.z;
        TopView.z = (TopRightPos.x - MinBottomLeftWorld.x) / DistanceMap.x;
        TopView.w = (TopRightPos.z - MinBottomLeftWorld.z) / DistanceMap.z;
        BottomView.x = (BottomLeftPos.x - MinBottomLeftWorld.x) / DistanceMap.x;
        BottomView.y = (BottomLeftPos.z - MinBottomLeftWorld.z) / DistanceMap.z;
        BottomView.z = (BottomRightPos.x - MinBottomLeftWorld.x) / DistanceMap.x;
        BottomView.w = (BottomRightPos.z - MinBottomLeftWorld.z) / DistanceMap.z;

        MiniMap Map = Game.GetService<MiniMap>();
        if (!Map)
            return;

        Map.PassData(TopView, BottomView);
    }

    private void UpdatePosition() {
        // Camera lateral movement
        Vector3 Movement = new Vector3(0, 0);
        if (Input.GetKey(KeyCode.W))
            Movement += new Vector3(+1, 0, +1);
        if (Input.GetKey(KeyCode.S))
            Movement += new Vector3(-1, 0, -1);
        if (Input.GetKey(KeyCode.A))
            Movement += new Vector3(-1, 0, +1);
        if (Input.GetKey(KeyCode.D))
            Movement += new Vector3(+1, 0, -1);

        if (Movement.sqrMagnitude > 0)
        {
            _OnCameraMoved.ForEach(_ => _.Invoke(TargetPosition));
        }

        Movement.Normalize();

        TargetPosition += Movement * Cam.orthographicSize * MovementSpeed;


        // Camera zoom
        float diff = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Approximately(diff, 0.0f))
            return;

        diff = diff > 0 ? -ZoomSteps : +ZoomSteps;
        float NewSize = Mathf.Clamp(Cam.orthographicSize + diff, MinimumZoom, MaximumZoom);
        Cam.orthographicSize = NewSize;
        OverlayCam.orthographicSize = NewSize;

        _OnCameraZoomed.ForEach(_ => _.Invoke(new(NewSize, 0, 0)));
    }

    private void MoveToPosition() {
        transform.position = Vector3.Lerp(transform.position, TargetPosition, PanningSpeed * Time.deltaTime);
    }

    public void TeleportTo(Vector3 TargetLocation)
    {
        TargetLocation.y = TargetPosition.y;
        TargetPosition = TargetLocation;
        transform.position = TargetLocation;
    }
    protected override void ResetInternal()
    {
        IsPaused = false;

    }

    private Camera Cam, OverlayCam;

    private bool IsPaused = false;

    public float MovementSpeed = 0.001f;
    public float PanningSpeed = 10;

    public float MinimumZoom = 15;
    public float MaximumZoom = 60;
    public float ZoomSteps = 5;

    public static Vector3 TargetPosition;

    public static ActionList<Vector3> _OnCameraMoved = new();
    public static ActionList<Vector3> _OnCameraZoomed = new();
}
