using UnityEngine;
using System.Collections.Generic;

public class PickupController : MonoBehaviour
{
    [Header("Pickup Settings")]
    public float pickupRange = 2f;
    public LayerMask pickupLayer;
    public Transform holdPoint;

    private GameObject heldItem;
    private Collider[] heldItemColliders;
    private Collider[] myColliders;
    private readonly List<(Collider, Collider)> ignoredCollisions = new();

    private void Awake()
    {
        myColliders = GetComponentsInChildren<Collider>();
    }

    public void TogglePickup()
    {
        if (heldItem)
        {
            DropItem();
        }
        else
        {
            PickUpItem();
        }
    }

    private void PickUpItem()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, pickupRange, pickupLayer, QueryTriggerInteraction.Collide);
        if (colliders.Length == 0) return;

        heldItem = colliders[0].gameObject;

        Rigidbody rb = heldItem.GetComponent<Rigidbody>();
        if (rb) rb.isKinematic = true;

        heldItemColliders = heldItem.GetComponentsInChildren<Collider>();
        foreach (Collider eagleCol in myColliders)
        {
            foreach (Collider itemCol in heldItemColliders)
            {
                Physics.IgnoreCollision(eagleCol, itemCol, true);
                ignoredCollisions.Add((eagleCol, itemCol));
            }
        }

        foreach (Collider col in heldItemColliders)
        {
            col.enabled = false;
        }

        if (holdPoint)
        {
            heldItem.transform.SetParent(holdPoint);
            heldItem.transform.localPosition = Vector3.zero;
            heldItem.transform.localRotation = Quaternion.identity;
        }
    }

    private void DropItem()
    {
        if (!heldItem) return;

        heldItem.transform.SetParent(null);

        Rigidbody rb = heldItem.GetComponent<Rigidbody>();
        if (rb) rb.isKinematic = false;

        foreach (Collider col in heldItemColliders)
        {
            col.enabled = true;
        }

        foreach (var pair in ignoredCollisions)
        {
            Physics.IgnoreCollision(pair.Item1, pair.Item2, false);
        }
        ignoredCollisions.Clear();

        heldItem = null;
        heldItemColliders = null;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRange);
    }
}
