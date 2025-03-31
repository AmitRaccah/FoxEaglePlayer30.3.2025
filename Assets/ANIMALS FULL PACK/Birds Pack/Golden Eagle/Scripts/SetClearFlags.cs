using UnityEngine;

public class SetClearFlags : MonoBehaviour
{
    void Start()
    {
        Camera cam = GetComponent<Camera>();
        if (cam != null)
        {
            // Force the overlay camera to clear only the depth buffer.
            cam.clearFlags = CameraClearFlags.Depth;
        }
    }
}
