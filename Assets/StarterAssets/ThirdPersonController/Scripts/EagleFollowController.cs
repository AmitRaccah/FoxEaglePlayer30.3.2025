using UnityEngine;

/// <summary>
/// Handles the eagle's follow behavior when direct control is disabled.
/// The eagle will smoothly follow a designated target (e.g., the player)
/// using a fixed offset and match its rotation. This version includes a noise effect 
/// to add subtle random variation to the eagle's follow movement.
/// </summary>
public class EagleFollowController : MonoBehaviour
{
    [Header("Follow Settings")]
    [Tooltip("The target to follow (e.g., the player).")]
    public Transform followTarget;
    [Tooltip("Offset relative to the target's position.")]
    public Vector3 followOffset = new Vector3(0, 3, -2);
    [Tooltip("Speed at which the eagle follows the target.")]
    public float followSpeed = 2f;

    [Header("Noise Settings")]
    [Tooltip("Speed at which the noise changes over time.")]
    public float noiseSpeed = 0.5f;
    [Tooltip("Maximum noise offset amplitude.")]
    public float noiseAmplitude = 1f;
    [Tooltip("Vertical scaling factor for noise.")]
    public float verticalNoiseFactor = 0.5f;

    private Vector3 currentVelocity = Vector3.zero;
    private float noiseTimerX, noiseTimerY, noiseTimerZ;

    void Update()
    {
        if (followTarget == null)
            return;

        FollowTarget();
    }

    /// <summary>
    /// Smoothly moves and rotates the eagle to follow the target,
    /// adding Perlin noise to the position for a subtle wandering effect.
    /// </summary>
    private void FollowTarget()
    {
        // Calculate the base desired position from the target and offset.
        Vector3 desiredPosition = followTarget.position + followOffset;

        // Update noise timers.
        noiseTimerX += noiseSpeed * Time.deltaTime;
        noiseTimerY += noiseSpeed * Time.deltaTime;
        noiseTimerZ += noiseSpeed * Time.deltaTime;

        // Calculate noise offsets using Perlin noise.
        float offsetX = (Mathf.PerlinNoise(noiseTimerX, 0f) - 0.5f) * noiseAmplitude;
        float offsetY = (Mathf.PerlinNoise(0f, noiseTimerY) - 0.5f) * noiseAmplitude * verticalNoiseFactor;
        float offsetZ = (Mathf.PerlinNoise(noiseTimerZ, 0f) - 0.5f) * noiseAmplitude;
        Vector3 noiseOffset = new Vector3(offsetX, offsetY, offsetZ);

        // Add the noise offset to the desired position.
        desiredPosition += noiseOffset;

        // Smoothly move the eagle to the desired position.
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, 1f / followSpeed);

        // Smoothly rotate to match the target's rotation.
        transform.rotation = Quaternion.Slerp(transform.rotation, followTarget.rotation, followSpeed * Time.deltaTime);
    }
}
