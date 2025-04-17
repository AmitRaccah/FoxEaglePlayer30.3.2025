using UnityEngine;

public class PickableItem : MonoBehaviour
{
    [Tooltip("Extra distance allowed if the item is large. " +
             "e.g., if Eagle's basePickupRange is 2, and this is 1, " +
             "the item can be grabbed up to distance = 3.")]
    public float additionalPickupRange = 0.5f;

    [Tooltip("Optional child transform that acts as the 'pivot' for pickup. " +
             "If null, Eagle will parent this entire object to its clawHoldPoint.")]
    public Transform pickupPivot;
}
