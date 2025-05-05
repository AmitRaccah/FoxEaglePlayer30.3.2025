using UnityEngine;

public class EagleController : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 5f;
    public float flyForce = 2f;

    [Header("References")]
    [SerializeField] private EagleInput input;
    [SerializeField] private EagleAltitudeHandler altitudeHandler;
    [SerializeField] private EagleMouseLook mouseLook;
    [SerializeField] private EagleBankingHandler bankingHandler;

    [SerializeField] private Transform yawPivot;    // Spine  (yaw only)
    [SerializeField] private Transform pitchPivot;  // Neck   (pitch only)
    [SerializeField] private EagleAnimationHandler animationHandler;


    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {

        bankingHandler.UpdateBanking(input.Data.banking);

        float dt = Time.deltaTime;
        EagleInputData d = input.Data;

        // altitude
        float newY = altitudeHandler.UpdateAltitude(d.ascend,
                                                    d.descend,
                                                    rb.position,
                                                    flyForce,
                                                    dt);

        // planar movement
        Vector3 nextPos = rb.position + d.moveDir * speed * dt;
        nextPos.y = newY;
        rb.MovePosition(nextPos);

        ApplyRotation();

        // stop residual physics drift
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        animationHandler.UpdateAnimation(
    input.Data,                          // current frame input
    false,                               // isGrounded – set true if you add ground check
    rb.linearVelocity.y                  // vertical speed
);

    }

    // --------------------------------------------------------------------
    private void ApplyRotation()
    {
        float pitch = mouseLook.CurrentPitch;
        float yaw = mouseLook.CurrentYaw;
        float roll = bankingHandler.CurrentRoll;

        pitchPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);

        Quaternion yawQ = Quaternion.Euler(0f, yaw, 0f);          // Yaw
        Quaternion rollQ = Quaternion.AngleAxis(roll, Vector3.forward); 
        transform.rotation = yawQ * rollQ;                         // (Yaw → Roll)
    }

}

