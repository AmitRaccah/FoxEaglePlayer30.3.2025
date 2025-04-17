using UnityEngine;
using StarterAssets;
using StarterAssets.Fox;
using System.Collections;


public class FoxFollowController : MonoBehaviour
{
    [Header("Target Settings")]
    [Tooltip("The main character (Player 1) that the fox should follow.")]
    [SerializeField] private Transform target;
    [Tooltip("Reference to the player's StarterAssetsInputs (used to mirror sprint state).")]
    [SerializeField] private StarterAssetsInputs playerInputs;

    [Header("Follow Distances")]
    [SerializeField] private float followDistance = 2f;
    [SerializeField] private float teleportThreshold = 20f;

    [Header("Follow Responsiveness")]
    [SerializeField] private float followResponsiveness = 5f;
    [SerializeField] private float runDistanceThreshold = 5f;

    [Header("Stop Zone Settings")]
    [SerializeField] private Transform stopZoneCenter;
    [SerializeField] private float stopZoneRadius = 3f;

    [Header("Obstacle Avoidance Settings")]
    [SerializeField] private LayerMask obstacleLayers;
    [SerializeField] private float obstacleSphereCastRadius = 0.5f;
    [SerializeField] private float obstacleCastDistance = 2f;
    [SerializeField] private float obstacleAvoidanceStrength = 0.5f;

    [Header("Miscellaneous Settings")]
    [SerializeField] private float gizmoSize = 0f;

    [SerializeField] private ParticleSystem teleportFireEffect;


    private FoxController foxController;
    private StarterAssetsInputs foxInputs;

    private void Awake()
    {
        foxController = GetComponent<FoxController>();
        foxInputs = GetComponent<StarterAssetsInputs>();
    }

    private void Start()
    {
        if (target == null || playerInputs == null)
        {
            enabled = false;
            return;
        }
        Invoke(nameof(InitialTeleportCheck), 0.1f);
    }

    private void OnEnable()
    {
        if (target != null)
            Invoke(nameof(InitialTeleportCheck), 0.1f);
    }

    private void Update()
    {
        if (target == null)
            return;

        // Teleport if too far away.
        if (Vector3.Distance(transform.position, target.position) > teleportThreshold)
        {
            TeleportToTarget();
            return;
        }

        // If within the stop zone, zero out movement.
        Vector3 zoneCenter = stopZoneCenter ? stopZoneCenter.position : target.position;
        if (Vector3.Distance(transform.position, zoneCenter) < stopZoneRadius)
        {
            foxInputs.move = Vector2.zero;
            foxInputs.sprint = false;
            return;
        }

        FollowTargetBehavior();
    }

    private void OnDrawGizmos()
    {
        if (target == null)
            return;

        Gizmos.color = Color.yellow;
        float drawSize = (gizmoSize > 0f) ? gizmoSize : stopZoneRadius;
        Vector3 center = stopZoneCenter ? stopZoneCenter.position : target.position;
        Gizmos.DrawWireSphere(center, drawSize);
    }

    private void InitialTeleportCheck()
    {
        if (target == null)
            return;
        if (Vector3.Distance(transform.position, target.position) > teleportThreshold)
            TeleportToTarget();
    }

    private void TeleportToTarget()
    {

        transform.position = target.position;
        //TP particle
        if (teleportFireEffect != null)
        {
            teleportFireEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            teleportFireEffect.Play();
            StartCoroutine(DisableEffectAfterDelay(teleportFireEffect, 1f)); // כמה זמן שהאפקט נמשך

        }


        transform.rotation = target.rotation;
        foxInputs.move = Vector2.zero;

    }

    private void FollowTargetBehavior()
    {
        Vector3 toTarget = target.position - transform.position;
        float distance = toTarget.magnitude;
        float diff = distance - followDistance;
        float forwardIntensity = diff > 0.1f ? Mathf.Clamp(diff, 0f, 1f) : 0f;
        Vector3 desiredDir = toTarget.normalized;
        desiredDir = AvoidObstacles(desiredDir, toTarget);
        float angleDiff = Vector3.SignedAngle(transform.forward, desiredDir, Vector3.up);
        float lateralInput = Mathf.Clamp(angleDiff / 90f, -1f, 1f);
        Vector2 simulatedInput = new Vector2(lateralInput, forwardIntensity);

        foxInputs.move = Vector2.Lerp(foxInputs.move, simulatedInput, Time.deltaTime * followResponsiveness);
        foxInputs.sprint = (distance > runDistanceThreshold) ? true : playerInputs.sprint;
    }

    private Vector3 AvoidObstacles(Vector3 desiredDir, Vector3 toTarget)
    {
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        float castDistance = Mathf.Min(obstacleCastDistance, toTarget.magnitude);
        if (Physics.SphereCast(origin, obstacleSphereCastRadius, desiredDir, out RaycastHit hit, castDistance, obstacleLayers))
        {
            Vector3 projectedDir = Vector3.ProjectOnPlane(desiredDir, hit.normal).normalized;
            desiredDir = Vector3.Slerp(desiredDir, projectedDir, obstacleAvoidanceStrength);
            if (Vector3.Dot(desiredDir, toTarget.normalized) < 0.5f)
                desiredDir = toTarget.normalized;
        }
        return desiredDir;
    }
    private IEnumerator DisableEffectAfterDelay(ParticleSystem effect, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (effect != null)
        {
            effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }


}
