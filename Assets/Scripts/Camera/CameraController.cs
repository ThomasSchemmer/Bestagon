using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{

    void Update() {
        Camera cam = Camera.main;

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

        this.transform.position = Vector3.Lerp(transform.position, transform.position + Movement * MovementSpeed * cam.orthographicSize, Time.deltaTime); ;

        // Camera zoom
        float diff = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Approximately(diff, 0.0f))
            return;

        diff = diff > 0 ? -ZoomSteps : +ZoomSteps;
        float NewSize = Mathf.Clamp(cam.orthographicSize + diff, MinimumZoom, MaximumZoom);
        cam.orthographicSize = NewSize;
    }

    public float MovementSpeed = 5;

    public float MinimumZoom = 15;
    public float MaximumZoom = 60;
    public float ZoomSteps = 5;
}
