using UnityEngine;
[RequireComponent(typeof(Camera))]
public sealed class MapCameraFit : MonoBehaviour
{
    private void LateUpdate()
    {
        Camera cam = GetComponent<Camera>();
        cam.orthographicSize = Mathf.Max(6.8f, 9.1f / Mathf.Max(0.1f, cam.aspect));
    }
}
