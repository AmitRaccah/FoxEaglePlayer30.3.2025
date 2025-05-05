using UnityEngine;

public class EagleAltitudeHandler : MonoBehaviour
{
    [SerializeField] private float targetAltitude;

    [SerializeField]
    private float minAltitude = 2f;

    [SerializeField]
    private float maxAltitude = 50f;

    [SerializeField]
    private float altitudeLerpSpeed = 10f;

    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float fallbackGroundY = 0f;

    private void Awake()
    {
        targetAltitude = transform.position.y;          // start from current height
    }

    private float GetGroundY(Vector3 position)
    {
        return Physics.Raycast(position, Vector3.down, out var hit, 1000f, groundLayer)
               ? hit.point.y
               : fallbackGroundY;
    }

    public float UpdateAltitude(bool ascend, bool descend,
                                Vector3 currentPosition,
                                float flyForce,
                                float dt)
    {
        float currentY = currentPosition.y;

        if (ascend)
            targetAltitude += flyForce * dt;
        else if (descend)
            targetAltitude -= flyForce * dt;
        else
            targetAltitude = currentY;                        // keep level when no input

        float groundY = GetGroundY(currentPosition);

        targetAltitude = Mathf.Clamp(targetAltitude,
                                     groundY + minAltitude,
                                     groundY + maxAltitude);

        float diff = Mathf.Abs(currentY - targetAltitude);
        float lerpT = diff < 0.05f ? 1f : altitudeLerpSpeed * dt;

        float newY = Mathf.Lerp(currentY, targetAltitude, lerpT);

        // safety clamp – never sink below ground + minAltitude
        float minY = groundY + minAltitude;
        if (newY < minY) newY = minY;

        return newY;
    }
}

