using UnityEngine;
using System.Collections.Generic;

public class FirstPersonMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float speed = 5f;
    public float runSpeed = 9f;
    public float smoothness = 0.95f;
    public float landingSmoothness = 0.8f;
    public float landingDuration = 0.3f;

    [Header("Slope Settings")]
    public float maxSlopeAngle = 45f;
    public float slopeInfluence = 1f;
    public float downhillSpeedMultiplier = 1.2f;
    public float uphillSpeedMultiplier = 0.8f;

    [Header("Inertia Settings")]
    public float inertiaFactor = 0.85f;
    public float minSpeedThreshold = 0.1f;
    public float directionalInertiaMultiplier = 1.5f;

    [Header("Air Control Settings")]
    public float airControlFactor = 0.3f; // How much control player has in air

    [Header("Landing Inertia Settings")]
    public float initialInertiaMultiplier = 1.2f;
    public float landingInertiaThreshold = 0.1f;
    public float landingDirectionChangeThreshold = 45f; // Angle threshold for direction change during landing
    public float postLandingInertiaFactor = 0.9f; // How strongly inertia affects direction changes after landing

    private Vector3 preGroundingVelocity;
    private bool hasLandingInertia;
    private Vector3 landingDirection; // Store the direction we landed in
    private float landingSpeed; // Store the speed we landed with


    private Rigidbody rigidbody;
    private Vector3 currentVelocity;
    private Vector3 previousMoveDirection;
    private bool isMovementLocked;
    private Vector3 lockedMoveDirection;
    private bool wasRunningWhenLocked;
    private GroundCheck groundCheck;
    private float landingTimer;
    private Vector3 landingVelocity;
    private bool isLanding;
    private RaycastHit slopeHit;
    private float currentSpeed;
    private Vector3 inertiaVelocity;
    private float jumpStartSpeed;

    public bool canRun = true;
    public bool IsRunning { get; private set; }
    public KeyCode runningKey = KeyCode.LeftShift;
    public List<System.Func<float>> speedOverrides = new List<System.Func<float>>();

    void Awake()
    {
        rigidbody = GetComponent<Rigidbody>();
        groundCheck = GetComponentInChildren<GroundCheck>();
        rigidbody.freezeRotation = true;
        previousMoveDirection = Vector3.zero;
    }

    void OnEnable() => groundCheck.Grounded += OnGrounded;
    void OnDisable() => groundCheck.Grounded -= OnGrounded;

    public void OnJumpStart()
    {
        jumpStartSpeed = IsRunning ? runSpeed : speed;

        if (currentVelocity.magnitude > minSpeedThreshold)
        {
            lockedMoveDirection = currentVelocity.normalized;
            isMovementLocked = true;
            wasRunningWhenLocked = true;
        }
    }

    bool IsOnSlope()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, groundCheck.distanceThreshold + 0.3f))
        {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            return angle != 0f && angle <= maxSlopeAngle;
        }
        return false;
    }

    Vector3 GetSlopeMoveDirection(Vector3 moveDirection)
    {
        return Vector3.ProjectOnPlane(moveDirection, slopeHit.normal).normalized;
    }

    void OnGrounded()
    {
        preGroundingVelocity = currentVelocity;

        landingDirection = preGroundingVelocity.normalized;
        landingSpeed = preGroundingVelocity.magnitude;

        hasLandingInertia = landingSpeed > landingInertiaThreshold;

        isMovementLocked = false;
        isLanding = true;
        landingTimer = landingDuration;

        if (hasLandingInertia)
        {
            landingVelocity = Vector3.ProjectOnPlane(preGroundingVelocity, Vector3.up).normalized * landingSpeed;
        }
        else
        {
            landingVelocity = Vector3.zero;
        }

        inertiaVelocity = landingVelocity;
        currentVelocity = landingVelocity;

        jumpStartSpeed = 0f;
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

    void FixedUpdate()
    {
        if (!groundCheck) return;

        IsRunning = canRun && Input.GetKey(runningKey);
        float targetSpeed = (!groundCheck.isGrounded && jumpStartSpeed > 0) ? jumpStartSpeed :
                          (IsRunning ? runSpeed : speed);

        Vector3 rawMoveDirection = GetMovementDirection();
        Vector3 worldMoveDirection = rawMoveDirection;
        float speedMultiplier = 1f;

        if (groundCheck.isGrounded && IsOnSlope())
        {
            worldMoveDirection = GetSlopeMoveDirection(worldMoveDirection);

            float dotProduct = Vector3.Dot(worldMoveDirection, Vector3.ProjectOnPlane(Vector3.up, slopeHit.normal));
            speedMultiplier = dotProduct > 0 ? uphillSpeedMultiplier :
                             dotProduct < 0 ? downhillSpeedMultiplier : 1f;

            worldMoveDirection *= slopeInfluence;
        }

        Vector3 targetVelocity = worldMoveDirection * (targetSpeed * speedMultiplier);

        if (isLanding)
        {
            HandleLandingWithInertia(rawMoveDirection);
        }
        else
        {
            if (groundCheck.isGrounded)
            {
                float directionDifference = Vector3.Angle(previousMoveDirection, worldMoveDirection);
                if (directionDifference > 30f && currentSpeed > minSpeedThreshold)
                {
                    inertiaVelocity = previousMoveDirection * currentSpeed * directionalInertiaMultiplier;
                }

                Vector3 blendedVelocity = Vector3.Lerp(targetVelocity, inertiaVelocity, inertiaFactor);
                currentVelocity = Vector3.Lerp(currentVelocity, blendedVelocity, 1f - smoothness);
            }
            else
            {
                Vector3 airVelocity = Vector3.Lerp(currentVelocity, targetVelocity, airControlFactor * Time.fixedDeltaTime);

                if (jumpStartSpeed > 0)
                {
                    Vector3 horizontalVelocity = new Vector3(airVelocity.x, 0, airVelocity.z);
                    if (horizontalVelocity.magnitude > 0.1f)
                    {
                        horizontalVelocity = horizontalVelocity.normalized * jumpStartSpeed;
                        airVelocity = new Vector3(horizontalVelocity.x, currentVelocity.y, horizontalVelocity.z);
                    }
                }

                currentVelocity = airVelocity;
            }

            inertiaVelocity = Vector3.Lerp(inertiaVelocity, targetVelocity, 1f - inertiaFactor);
        }

        currentSpeed = currentVelocity.magnitude;
        if (worldMoveDirection.magnitude > 0.1f)
        {
            previousMoveDirection = worldMoveDirection.normalized;
        }

        Vector3 newPosition = rigidbody.position + (currentVelocity * Time.deltaTime);
        if (!IsOnSlope())
        {
            newPosition.y = rigidbody.position.y;
        }

        rigidbody.MovePosition(newPosition);
    }

    private void HandleLandingWithInertia(Vector3 inputDirection)
    {
        landingTimer -= Time.fixedDeltaTime;
        
        if (landingTimer <= 0)
        {
            isLanding = false;
            currentVelocity = inertiaVelocity;
        }
        else
        {
            float t = 1 - (landingTimer / landingDuration);
            
            if (inputDirection.magnitude > 0.1f)
            {
                float directionChange = Vector3.Angle(landingDirection, inputDirection);
                bool isSignificantDirectionChange = directionChange > landingDirectionChangeThreshold;

                float targetSpeed = Mathf.Min(landingSpeed, IsRunning ? runSpeed : speed);
                Vector3 targetVelocity = inputDirection * targetSpeed;

                if (isSignificantDirectionChange && hasLandingInertia)
                {
                    float inertiaStrength = Mathf.Lerp(postLandingInertiaFactor, 0, t);
                    Vector3 inertiaDirection = Vector3.Lerp(landingDirection, inputDirection, t);
                    Vector3 blendedVelocity = Vector3.Lerp(
                        targetVelocity,
                        inertiaDirection * landingSpeed,
                        inertiaStrength
                    );
                    currentVelocity = Vector3.Lerp(currentVelocity, blendedVelocity, 1f - smoothness);
                }
                else
                {
                    Vector3 blendedDirection = Vector3.Lerp(landingDirection, inputDirection, t);
                    currentVelocity = blendedDirection * Mathf.Lerp(landingSpeed, targetSpeed, t);
                }
            }
            else
            {
                if (hasLandingInertia)
                {
                    float decayedSpeed = Mathf.Lerp(landingSpeed, 0, t * landingSmoothness);
                    currentVelocity = landingDirection * decayedSpeed;
                }
                else
                {
                    currentVelocity = Vector3.zero;
                }
            }
        }
        
        inertiaVelocity = currentVelocity;
    }

    private Vector3 GetMovementDirection()
    {
        if (isMovementLocked && !groundCheck.isGrounded)
        {
            return lockedMoveDirection;
        }

        Vector3 moveDirection = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized;
        return transform.TransformDirection(moveDirection);
    }
}