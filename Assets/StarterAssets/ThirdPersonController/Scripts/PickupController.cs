using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Collider))]
public class PickupController : MonoBehaviour
{
    [Header("Pickup Settings")]
    public Transform holdPoint;
    public float basePickupRange = 2f;
    public LayerMask pickupLayer;
    public float throwForce = 5f;

    private bool isCarryingItem = false;
    private Rigidbody carriedItemRb = null;
    private List<Collider> disabledColliders = new List<Collider>();

    /// <summary>
    /// Toggles the pickup state.
    /// </summary>
    public void TogglePickup()
    {
        if (isCarryingItem)
            ReleaseItem();
        else
            TryPickupItem();
    }


    /// <summary>
    /// Attempts to pick up the nearest valid item.
    /// </summary>
    public void TryPickupItem()
    {
        Transform pickupCenter = holdPoint ? holdPoint : transform;
        Collider[] hits = Physics.OverlapSphere(pickupCenter.position, basePickupRange, pickupLayer, QueryTriggerInteraction.Collide);
        Collider closest = null;
        float minDist = Mathf.Infinity;
        foreach (Collider col in hits)
        {
            // Skip self colliders.
            if (col.transform.IsChildOf(transform) || col.gameObject == gameObject)
                continue;
            float distance = Vector3.Distance(pickupCenter.position, col.transform.position);
            if (distance <= basePickupRange && distance < minDist)
            {
                minDist = distance;
                closest = col;
            }
        }
        if (closest == null)
            return;
        if (closest.GetComponent<Rigidbody>() is not Rigidbody itemRb)
            return;

        carriedItemRb = itemRb;
        carriedItemRb.isKinematic = true;
        carriedItemRb.useGravity = false;
        disabledColliders.Clear();
        foreach (Collider col in carriedItemRb.GetComponentsInChildren<Collider>())
        {
            if (col.enabled)
            {
                col.enabled = false;
                disabledColliders.Add(col);
            }
        }
        carriedItemRb.transform.SetParent(pickupCenter);
        carriedItemRb.transform.localPosition = Vector3.zero;
        carriedItemRb.transform.localRotation = Quaternion.identity;
        isCarryingItem = true;
    }

    /// <summary>
    /// Releases the carried item.
    /// </summary>
    public void ReleaseItem()
    {
        if (!isCarryingItem || carriedItemRb == null)
            return;
        carriedItemRb.transform.SetParent(null);
        carriedItemRb.isKinematic = false;
        carriedItemRb.useGravity = true;
        foreach (Collider col in disabledColliders)
        {
            if (col != null)
                col.enabled = true;
        }
        disabledColliders.Clear();
        carriedItemRb.AddForce(transform.forward * throwForce, ForceMode.Impulse);
        carriedItemRb = null;
        isCarryingItem = false;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Transform pickupCenter = holdPoint ? holdPoint : transform;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(pickupCenter.position, basePickupRange);
    }
#endif
}
