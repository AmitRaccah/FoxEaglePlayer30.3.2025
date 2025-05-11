using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Respawn : MonoBehaviour
{
    [Header("Who should be teleported")]
    [Tooltip("Layers that trigger a respawn when they enter this trigger")]
    [SerializeField] private LayerMask respawnMask = ~0;   // Everything by default

    [Header("Where to send them")]
    [SerializeField] private Transform respawnPoint;

    [Tooltip("Keep the original rotation instead of copying the point’s rotation")]
    [SerializeField] private bool keepOriginalRotation = true;

    /* -------------------------------------------------------- */
    private void OnTriggerEnter(Collider other)
    {
        // Exit if the object's layer is NOT inside the mask
        if ((respawnMask.value & (1 << other.gameObject.layer)) == 0)
            return;

        Debug.Log($"[Respawn] {other.name} → teleport");
        Teleport(other.transform.root);          // handle child colliders too
    }

    /* ------------------ helpers ------------------ */

    private void Teleport(Transform obj)
    {
        if (!respawnPoint) return;

        obj.position = respawnPoint.position;
        if (!keepOriginalRotation)
            obj.rotation = respawnPoint.rotation;

        // clear physics velocity
        if (obj.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // reset CharacterController to avoid “stuck” offset after teleport
        if (obj.TryGetComponent<CharacterController>(out var cc))
        {
            cc.enabled = false;
            cc.enabled = true;
        }
    }
}
