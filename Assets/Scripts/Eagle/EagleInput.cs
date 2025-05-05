using UnityEngine;

public class EagleInput : MonoBehaviour
{
    public Vector3 moveDirection { get; private set; }
    public bool isAscending { get; private set; }
    public bool isDescending { get; private set; }

    [SerializeField] private Transform orientation;

    public EagleInputData Data { get; private set; }

    private void Update()
    {
        float verticalInput = Input.GetAxisRaw("Vertical");
        float horizontalInput = Input.GetAxisRaw("Horizontal");

        float bankingInput = 0f;
        if (Input.GetKey(KeyCode.Q)) bankingInput = 1f;
        else if (Input.GetKey(KeyCode.E)) bankingInput = -1f;

        Vector3 forward = new Vector3(orientation.forward.x, 0f, orientation.forward.z).normalized;
        Vector3 right = new Vector3(orientation.right.x, 0f, orientation.right.z).normalized;

        moveDirection = (right * horizontalInput) + (forward * verticalInput);
        isAscending = Input.GetKey(KeyCode.Space);
        isDescending = Input.GetKey(KeyCode.LeftControl);

        Data = new EagleInputData
        {
            moveDir = moveDirection.normalized,
            ascend = isAscending,
            descend = isDescending,
            banking = bankingInput
        };
    }
}
