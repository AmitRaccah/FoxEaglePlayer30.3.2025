using UnityEngine;
using UnityEngine.InputSystem; // Using new Input System

/// <summary>
/// A simple input handler for triggering the pickup functionality.
/// Attach this script (along with PickupController) to your character's GameObject.
/// </summary>
public class PickupInput : MonoBehaviour
{
    private PickupController pickupController;

    private void Awake()
    {
        pickupController = GetComponent<PickupController>();
        if (pickupController == null)
        {
            Debug.LogError("[PickupInput] PickupController component not found on this GameObject!");
        }
    }

    private void Update()
    {
        // Use new Input System if available; otherwise, fallback to legacy input.
        if (Mouse.current != null)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                Debug.Log("[PickupInput] Left mouse button pressed (new Input System).");
                pickupController.TogglePickup();
            }
        }
        else
        {
            if (Input.GetMouseButtonDown(0))
            {
                Debug.Log("[PickupInput] Left mouse button pressed (legacy input).");
                pickupController.TogglePickup();
            }
        }
    }
}
