using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Penguin Ice Movement")]
    [SerializeField] private float moveForce = 18f;
    [SerializeField] private float maxVelocity = 12f;
    [SerializeField] private float maxDownhillSpeed = 22f; // High top speed when sliding downhill
    [SerializeField] private float downhillBoost = 25f; // Acceleration boost when sliding down slopes
    [SerializeField] private float iceDrag = 0.5f; // Low friction for ice sliding coast feel

    [Header("Jump Settings")]
    [SerializeField] private float jumpForce = 9f;
    [SerializeField] private int maxJumps = 2;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckDistance = 0.6f;
    [SerializeField] private float groundCheckWidth = 0.25f;
    [SerializeField] private float coyoteTime = 0.15f; // Grace period after leaving ground to still jump
    [SerializeField] private float jumpBufferTime = 0.15f; // Buffer jump input before hitting ground

    [Header("Slope & Rotation Alignment")]
    [SerializeField] private bool rotateToSlope = true;
    [SerializeField] private float slopeRotationSpeed = 20f; // Fast, smooth alignment with ice slopes
    [SerializeField] private float maxSlopeAngle = 60f;
    [SerializeField] private float slopeSnapForce = 15f; // Downward stickiness force when sliding downhill at speed
    [SerializeField] private float rampLaunchMultiplier = 1.25f; // Launch momentum boost when leaving an upward slope

    [Header("Air Pitch Rotation Settings")]
    [SerializeField] private bool rotateInAir = true;
    [SerializeField] private float airRotationSpeed = 8f;
    [SerializeField] private float maxAirPitchDown = -60f; // Max nose-down tilt when falling towards ground
    [SerializeField] private float maxAirPitchUp = 60f; // Max nose-up tilt when launching upward

    [Header("Flat Ground & Backward Stopping")]
    [SerializeField] private float flatGroundBrake = 8f; // Natural deceleration on flat ground when releasing D key
    [SerializeField] private float negativeVelStopDuration = 0.5f; // Duration before auto-stopping backward movement (0.5s default)
    [SerializeField] private float negativeVelBrakeForce = 30f; // Smooth rapid brake force when stopping negative velocity

    [Header("Juice / Visual Feedback")]
    [SerializeField] private bool enableJuice = true;
    [SerializeField] private Vector3 jumpPunchScale = new Vector3(-0.15f, 0.25f, 0f);
    [SerializeField] private Vector3 landPunchScale = new Vector3(0.25f, -0.2f, 0f);
    [SerializeField] private float juiceDuration = 0.2f;

    private Rigidbody2D rb;
    private bool jumpRequested;
    private int jumpsRemaining;
    private bool isGrounded;
    private Vector2 groundNormal = Vector2.up;
    private float negativeVelTimer = 0f;
    private float coyoteTimer = 0f;
    private float jumpBufferTimer = 0f;
    private float justJumpedTimer = 0f;
    private bool wasGroundedOnSlope = false;
    private Vector2 lastSlopeTangent = Vector2.right;
    private Vector3 initialScale = Vector3.one;

    public bool IsGrounded => isGrounded;
    public Vector2 GroundNormal => groundNormal;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        initialScale = transform.localScale;
    }

    private void OnDestroy()
    {
        transform.DOKill();
    }

    private void TriggerJuice(Vector3 punchAmount)
    {
        if (!enableJuice) return;
        transform.DOKill();
        transform.localScale = initialScale;
        transform.DOPunchScale(punchAmount, juiceDuration, 6, 0.5f);
    }

    private void Update()
    {
        if (Input.GetButtonDown("Jump"))
        {
            jumpBufferTimer = jumpBufferTime;
        }
        else
        {
            jumpBufferTimer -= Time.deltaTime;
        }
    }

    private void CheckGrounded()
    {
        justJumpedTimer -= Time.fixedDeltaTime;

        // If player just initiated a jump, skip ground check briefly so jump force isn't canceled
        if (justJumpedTimer > 0f && rb.linearVelocity.y > 0.1f)
        {
            isGrounded = false;
            groundNormal = Vector2.up;
            coyoteTimer -= Time.fixedDeltaTime;
            return;
        }

        Vector3 leftOrigin = transform.position + Vector3.left * groundCheckWidth;
        Vector3 rightOrigin = transform.position + Vector3.right * groundCheckWidth;

        RaycastHit2D hitLeft = Physics2D.Raycast(leftOrigin, Vector2.down, groundCheckDistance, groundLayer);
        RaycastHit2D hitRight = Physics2D.Raycast(rightOrigin, Vector2.down, groundCheckDistance, groundLayer);

        bool leftGrounded = hitLeft.collider != null;
        bool rightGrounded = hitRight.collider != null;

        bool currentlyGrounded = leftGrounded || rightGrounded;

        if (currentlyGrounded)
        {
            if (leftGrounded && rightGrounded)
            {
                groundNormal = ((hitLeft.normal + hitRight.normal) * 0.5f).normalized;
            }
            else if (leftGrounded)
            {
                groundNormal = hitLeft.normal;
            }
            else
            {
                groundNormal = hitRight.normal;
            }

            if (!isGrounded)
            {
                TriggerJuice(landPunchScale);
            }

            isGrounded = true;
            coyoteTimer = coyoteTime;
            jumpsRemaining = maxJumps;
        }
        else
        {
            // Ramp Launching check: if we were grounded on a slope in previous frame and moving fast, apply launch boost!
            if (isGrounded && wasGroundedOnSlope && rb.linearVelocity.magnitude > 5f)
            {
                Vector2 launchImpulse = lastSlopeTangent * (rb.linearVelocity.magnitude * (rampLaunchMultiplier - 1f));
                if (launchImpulse.y > 0f)
                {
                    rb.AddForce(launchImpulse, ForceMode2D.Impulse);
                }
            }

            isGrounded = false;
            coyoteTimer -= Time.fixedDeltaTime;
            groundNormal = Vector2.up;
        }
    }

    private void FixedUpdate()
    {
        CheckGrounded();

        // 1. Jump Execution with Coyote Time & Jump Buffer
        bool canJumpFromGround = (isGrounded || coyoteTimer > 0f) && jumpsRemaining == maxJumps;
        bool canAirJump = !isGrounded && coyoteTimer <= 0f && jumpsRemaining > 0;

        if (jumpBufferTimer > 0f && (canJumpFromGround || canAirJump))
        {
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
            justJumpedTimer = 0.15f;

            // Trigger Jump Stretch Visual Feedback
            TriggerJuice(jumpPunchScale);

            // Launch direction blends Upward force with current slope normal
            Vector2 jumpDir = (Vector2.up * 0.8f + groundNormal * 0.2f).normalized;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            rb.AddForce(jumpDir * jumpForce, ForceMode2D.Impulse);

            jumpsRemaining--;
            isGrounded = false;
            groundNormal = Vector2.up;
        }

        // 2. Smooth Z-Rotation Alignment (Slope Grounded & Mid-Air Pitching)
        if (rotateToSlope)
        {
            float targetZAngle = 0f;
            float currentRotationSpeed = slopeRotationSpeed;

            if (isGrounded)
            {
                targetZAngle = Vector2.SignedAngle(Vector2.up, groundNormal);
                targetZAngle = Mathf.Clamp(targetZAngle, -maxSlopeAngle, maxSlopeAngle);
            }
            else if (rotateInAir)
            {
                currentRotationSpeed = airRotationSpeed;
                Vector2 vel = rb.linearVelocity;

                if (vel.magnitude > 0.5f)
                {
                    // Calculate pitch angle based on airborne movement vector (nosing down when falling)
                    float velAngle = Vector2.SignedAngle(Vector2.right, vel);
                    targetZAngle = Mathf.Clamp(velAngle, maxAirPitchDown, maxAirPitchUp);
                }
            }

            float newAngle = Mathf.LerpAngle(rb.rotation, targetZAngle, currentRotationSpeed * Time.fixedDeltaTime);
            rb.MoveRotation(newAngle);
        }

        // 3. Movement & Ice Slope Sliding Physics
        float moveX = Input.GetAxisRaw("Horizontal");
        if (moveX < 0f) moveX = 0f; // Disable A / Left Arrow key input

        float surfaceAngle = Vector2.Angle(groundNormal, Vector2.up);
        bool isOnSlope = isGrounded && surfaceAngle > 3f;

        // Calculate slope downhill direction
        Vector2 downhillTangent = new Vector2(groundNormal.y, -groundNormal.x);
        if (downhillTangent.y > 0f) downhillTangent = -downhillTangent;

        bool isGoingDownhill = isOnSlope && downhillTangent.x > 0f;
        bool isSlidingDownhillLeft = isOnSlope && downhillTangent.x < 0f;

        if (isGrounded)
        {
            wasGroundedOnSlope = isOnSlope;
            lastSlopeTangent = (downhillTangent.x > 0f) ? -downhillTangent : downhillTangent;

            if (isGoingDownhill)
            {
                // Downhill Ice Slide Boost: Acceleration increases along downhill slope
                float slopeFactor = Mathf.Sin(surfaceAngle * Mathf.Deg2Rad);
                Vector2 slideForce = downhillTangent * (downhillBoost * slopeFactor);

                // Additional push if player presses right
                if (moveX > 0f) slideForce += downhillTangent * moveForce;

                // Downhill Slope Snap (keep stuck to slope)
                slideForce += -groundNormal * slopeSnapForce;

                rb.AddForce(slideForce, ForceMode2D.Force);

                // Cap max downhill velocity
                if (rb.linearVelocity.magnitude > maxDownhillSpeed)
                {
                    rb.linearVelocity = rb.linearVelocity.normalized * maxDownhillSpeed;
                }
            }

            else if (!Mathf.Approximately(moveX, 0f))
            {
                // Normal horizontal movement / Uphill push
                Vector2 forceDir = isOnSlope ? -downhillTangent : Vector2.right;
                rb.AddForce(forceDir * moveForce, ForceMode2D.Force);

                if (Mathf.Abs(rb.linearVelocity.x) > maxVelocity)
                {
                    rb.linearVelocity = new Vector2(Mathf.Sign(rb.linearVelocity.x) * maxVelocity, rb.linearVelocity.y);
                }
            }
            else
            {
                // Flat ground auto-deceleration when releasing D key
                Vector2 vel = rb.linearVelocity;
                float decel = (!isOnSlope) ? flatGroundBrake : iceDrag;
                if (Mathf.Abs(vel.x) > 0.05f)
                {
                    float newVelX = Mathf.MoveTowards(vel.x, 0f, decel * Time.fixedDeltaTime);
                    rb.linearVelocity = new Vector2(newVelX, vel.y);
                }
            }
        }
        else
        {
            // Air Control
            if (!Mathf.Approximately(moveX, 0f))
            {
                rb.AddForce(Vector2.right * (moveX * moveForce * 0.5f), ForceMode2D.Force);
            }
        }

        // 4. Auto-stop negative linear velocity X (only when grounded, not sliding downhill left, and no horizontal input)
        float rawInput = Input.GetAxisRaw("Horizontal");
        if (isGrounded && !isSlidingDownhillLeft && rb.linearVelocity.x < -0.01f && Mathf.Approximately(rawInput, 0f))
        {
            negativeVelTimer += Time.fixedDeltaTime;
            if (negativeVelTimer >= negativeVelStopDuration)
            {
                float smoothVelX = Mathf.MoveTowards(rb.linearVelocity.x, 0f, negativeVelBrakeForce * Time.fixedDeltaTime);
                rb.linearVelocity = new Vector2(smoothVelX, rb.linearVelocity.y);
            }
        }
        else
        {
            negativeVelTimer = 0f;
        }
    }



    private void OnDrawGizmosSelected()
    {
        Vector3 leftOrigin = transform.position + Vector3.left * groundCheckWidth;
        Vector3 rightOrigin = transform.position + Vector3.right * groundCheckWidth;

        Vector3 leftTarget = leftOrigin + Vector3.down * groundCheckDistance;
        Vector3 rightTarget = rightOrigin + Vector3.down * groundCheckDistance;

        bool isPlayMode = Application.isPlaying;
        Color checkColor = (isPlayMode && isGrounded) ? Color.green : Color.red;

        Gizmos.color = checkColor;

        // Raycast lines
        Gizmos.DrawLine(leftOrigin, leftTarget);
        Gizmos.DrawLine(rightOrigin, rightTarget);

        // Raycast origins
        Gizmos.DrawWireSphere(leftOrigin, 0.05f);
        Gizmos.DrawWireSphere(rightOrigin, 0.05f);

        // Raycast endpoints
        Gizmos.DrawWireSphere(leftTarget, 0.03f);
        Gizmos.DrawWireSphere(rightTarget, 0.03f);

        // Draw ground normal vector & downhill tangent when playing and grounded
        if (isPlayMode && isGrounded)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position, groundNormal * 1.5f);

            Vector2 downhillTangent = new Vector2(groundNormal.y, -groundNormal.x);
            if (downhillTangent.y > 0f) downhillTangent = -downhillTangent;
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position, downhillTangent * 1.5f);
        }
    }
}

