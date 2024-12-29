using UnityEngine;

public class CameraSway : MonoBehaviour
{
    public float normalSwayAmount = 0.02f;
    public float sprintSwayAmount = 0.04f;
    public float normalSwaySpeed = 20f;
    public float sprintSwaySpeed = 30f;
    public float normalSwaySideAmount = 0.03f;
    public float sprintSwaySideAmount = 0.06f;
    public float minSpeedThreshold = 0.1f;

    private Vector3 originalPosition;
    private bool isMoving;
    private CharacterController characterController;
    [SerializeField] private GroundCheck groundCheck;

    void Start()
    {
        originalPosition = transform.localPosition;
        characterController = GetComponentInParent<CharacterController>();

        if (groundCheck == null)
        {
            groundCheck = GetComponentInParent<GroundCheck>();
        }
    }

    void Update()
    {
        if (groundCheck == null || !groundCheck.isGrounded)
        {
            transform.localPosition = Vector3.Lerp(transform.localPosition, originalPosition, Time.deltaTime * 5f);
            return;
        }

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 movementSpeed = characterController != null
            ? characterController.velocity
            : new Vector3(horizontal, 0, vertical);
        float currentSpeed = new Vector2(movementSpeed.x, movementSpeed.z).magnitude;

        isMoving = currentSpeed > minSpeedThreshold;
        bool isSprinting = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        if (isMoving)
        {
            float currentSwayAmount = isSprinting ? sprintSwayAmount : normalSwayAmount;
            float currentSwaySideAmount = isSprinting ? sprintSwaySideAmount : normalSwaySideAmount;
            float currentSwaySpeed = isSprinting ? sprintSwaySpeed : normalSwaySpeed;

            float swayOffset = Mathf.Sin(Time.time * currentSwaySpeed) * currentSwayAmount;
            float sideSwayOffset = Mathf.Cos(Time.time * currentSwaySpeed * 0.5f) * currentSwaySideAmount;

            transform.localPosition = originalPosition + new Vector3(sideSwayOffset, swayOffset, 0);
        }
        else
        {
            transform.localPosition = Vector3.Lerp(transform.localPosition, originalPosition, Time.deltaTime * 5f);
        }
    }
}