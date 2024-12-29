using System.Collections.Generic;
using UnityEngine;

public class FirstPersonMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float speed = 3;
    public float smoothness = 0.95f;
    public float landingSmoothness = 0.8f;
    public float landingDuration = 0.3f;

    private Rigidbody rigidbody;
    private Vector3 currentVelocity;
    private bool isMovementLocked;
    private Vector3 lockedMoveDirection;
    private bool wasRunningWhenLocked;
    private GroundCheck groundCheck;
    private float landingTimer;
    private Vector3 landingVelocity;
    private bool isLanding;

    void Awake()
    {
        rigidbody = GetComponent<Rigidbody>();
        groundCheck = GetComponentInChildren<GroundCheck>();
        rigidbody.freezeRotation = true;
    }

    public void LockMovementDirection()
    {
        Vector3 moveInput = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized;
        if (moveInput.magnitude > 0.1f)
        {
            lockedMoveDirection = transform.TransformDirection(moveInput);
            wasRunningWhenLocked = IsRunning;
            isMovementLocked = true;
        }
    }

    void OnEnable() => groundCheck.Grounded += OnGrounded;
    void OnDisable() => groundCheck.Grounded -= OnGrounded;

    void OnGrounded()
    {
        isMovementLocked = false;
        isLanding = true;
        landingTimer = landingDuration;
        landingVelocity = currentVelocity;
    }

    void FixedUpdate()
    {
        if (!groundCheck) return;

        IsRunning = canRun && Input.GetKey(runningKey);
        float targetSpeed = IsRunning ? runSpeed : speed;

        if (isLanding)
        {
            landingTimer -= Time.fixedDeltaTime;
            if (landingTimer <= 0)
            {
                isLanding = false;
                currentVelocity = Vector3.zero;
            }
            else
            {
                float t = 1 - (landingTimer / landingDuration);
                currentVelocity = Vector3.Lerp(landingVelocity, Vector3.zero, t * landingSmoothness);
            }
        }
        else
        {
            Vector3 worldMoveDirection;
            if (isMovementLocked && !groundCheck.isGrounded)
            {
                worldMoveDirection = lockedMoveDirection;
                targetSpeed = wasRunningWhenLocked ? runSpeed : speed;
            }
            else
            {
                Vector3 moveDirection = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized;
                worldMoveDirection = transform.TransformDirection(moveDirection);
            }

            Vector3 targetVelocity = worldMoveDirection * targetSpeed;
            currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, (1f - smoothness));
        }

        Vector3 newPosition = rigidbody.position + (currentVelocity * Time.deltaTime);
        newPosition.y = rigidbody.position.y;
        rigidbody.MovePosition(newPosition);
    }

    public bool canRun = true;
    public bool IsRunning { get; private set; }
    public float runSpeed = 7;
    public KeyCode runningKey = KeyCode.LeftShift;
    public List<System.Func<float>> speedOverrides = new List<System.Func<float>>();
}