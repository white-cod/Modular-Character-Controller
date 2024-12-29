using UnityEngine;

public class Jump : MonoBehaviour
{
    Rigidbody rigidbody;
    public float jumpStrength = 40;
    public event System.Action Jumped;
    [SerializeField] GroundCheck groundCheck;
    FirstPersonMovement movement;

    void Reset()
    {
        groundCheck = GetComponentInChildren<GroundCheck>();
    }

    void Awake()
    {
        rigidbody = GetComponent<Rigidbody>();
        movement = GetComponent<FirstPersonMovement>();
    }

    void Update()
    {
        if (Input.GetButtonDown("Jump") && groundCheck && groundCheck.isGrounded)
        {
            rigidbody.linearVelocity = new Vector3(rigidbody.linearVelocity.x, 0, rigidbody.linearVelocity.z);
            rigidbody.AddForce(Vector3.up * 100 * jumpStrength);
            movement.LockMovementDirection();
            Jumped?.Invoke();
        }
    }
}