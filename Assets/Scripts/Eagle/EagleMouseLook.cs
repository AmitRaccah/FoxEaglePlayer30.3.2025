using UnityEngine;

public class EagleMouseLook : MonoBehaviour
{
    [SerializeField] private float minPitch = -45f;
    [SerializeField] private float maxPitch = 45f;
    [SerializeField] private Transform yawPivot;
    [SerializeField] private Transform pitchPivot;
    [SerializeField] private float sensitivity = 1f;

    private float pitch;
    private float yaw;

    public float CurrentPitch => pitch;
    public float CurrentYaw => yaw;

    private void Start()
    {
        pitch = pitchPivot.localEulerAngles.x;
        yaw = yawPivot.eulerAngles.y;
    }


    private void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * sensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity;

        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

    }
}

