using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    private void Start() {
        Cam = GetComponent<Camera>();
    }


    void Update() {
        UpdatePosition();
        UpdateMiniMapData();
    }

    private void UpdateMiniMapData() {
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

        MapGenerator.GetMapBounds(out Location BottomLeftMap, out Location TopRightMap);
        Vector2 BottomLeftMapPos = new Vector2(BottomLeftMap.WorldLocation.x, BottomLeftMap.WorldLocation.z);
        Vector2 TopRightMapPos = new Vector2(TopRightMap.WorldLocation.x, TopRightMap.WorldLocation.z); ;
        Vector2 DistanceMap = TopRightMapPos - BottomLeftMapPos;

        Vector3 TopLeftPos = TopLeftRay.GetPoint(TopLeftScale);
        Vector3 TopRightPos = TopRightRay.GetPoint(TopRightScale);
        Vector3 BottomRightPos = BottomRightRay.GetPoint(BottomRightScale);
        Vector3 BottomLeftPos = BottomLeftRay.GetPoint(BottomLeftScale);

        // each vector4 contains two uv positions at each (x, y) and (z, w)
        // the pos vectors for the frustum edges are still in (x, y, z) coordinates!
        Vector4 TopView = new Vector4();
        Vector4 BottomView = new Vector4();
        TopView.x = (TopLeftPos.x - BottomLeftMapPos.x) / DistanceMap.x;
        TopView.y = (TopLeftPos.z - BottomLeftMapPos.y) / DistanceMap.y;
        TopView.z = (TopRightPos.x - BottomLeftMapPos.x) / DistanceMap.x;
        TopView.w = (TopRightPos.z - BottomLeftMapPos.y) / DistanceMap.y;
        BottomView.x = (BottomLeftPos.x - BottomLeftMapPos.x) / DistanceMap.x;
        BottomView.y = (BottomLeftPos.z - BottomLeftMapPos.y) / DistanceMap.y;
        BottomView.z = (BottomRightPos.x - BottomLeftMapPos.x) / DistanceMap.x;
        BottomView.w = (BottomRightPos.z - BottomLeftMapPos.y) / DistanceMap.y;

        // ugly direct call
        MiniMap.Instance.MiniMapRT.material.SetVector("_TopView", TopView);
        MiniMap.Instance.MiniMapRT.material.SetVector("_BottomView", BottomView);
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

        Movement.Normalize();

        this.transform.position = Vector3.Lerp(transform.position, transform.position + Movement * MovementSpeed * Cam.orthographicSize, Time.deltaTime); ;

        // Camera zoom
        float diff = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Approximately(diff, 0.0f))
            return;

        diff = diff > 0 ? -ZoomSteps : +ZoomSteps;
        float NewSize = Mathf.Clamp(Cam.orthographicSize + diff, MinimumZoom, MaximumZoom);
        Cam.orthographicSize = NewSize;
    }

    private Camera Cam;

    public float MovementSpeed = 5;

    public float MinimumZoom = 15;
    public float MaximumZoom = 60;
    public float ZoomSteps = 5;
}
