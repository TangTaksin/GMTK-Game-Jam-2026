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

    [Header("Sprite Settings")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite defaultSprite;
    [SerializeField] private Sprite idleSprite;
    [SerializeField] private Sprite jumpSprite;

    [Header("Dust Particle Settings")]
    [SerializeField] private bool enableDustParticles = true;
    [SerializeField] private Vector3 feetOffset = new Vector3(0f, -0.5f, 0f);
    [SerializeField] private ParticleSystem jumpDustPrefab;
    [SerializeField] private ParticleSystem landDustPrefab;

    [Header("Jump Intro Settings")]
    [Tooltip("Enable jump intro animation when game starts.")]
    [SerializeField] private bool enableJumpIntro = true;
    [Tooltip("Delay in seconds before starting the jump intro animation (Set lower than ChasingThreat so player jumps first).")]
    [SerializeField] private float jumpIntroDelay = 0.05f;
    [Tooltip("Peak jump height above ground surface.")]
    [SerializeField] private float jumpHeight = 3.5f;
    [Tooltip("Initial Y offset below ground surface before jump.")]
    [SerializeField] private float startYOffset = -5.0f;
    [Tooltip("Initial X offset relative to starting position.")]
    [SerializeField] private float startXOffset = -3.0f;
    [Tooltip("Landing X offset relative to starting position (0 = land on spawn position).")]
    [SerializeField] private float landingXOffset = 0.0f;
    [Tooltip("Total duration of jump animation in seconds.")]
    [SerializeField] private float jumpDuration = 0.85f;
    [Tooltip("Rotation pitch effect during jump (degrees).")]
    [SerializeField] private float jumpPitchAngle = 20f;
    [SerializeField] private Ease jumpUpEase = Ease.OutQuad;
    [SerializeField] private Ease jumpDownEase = Ease.InQuad;

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

    // Intro Jump State Variables
    private bool isIntroJumping;
    private float baseSpawnX;
    private float currentJumpYOffset;
    private float currentJumpXOffset;
    private float currentJumpPitch;
    private Sequence jumpSequence;

    public bool IsGrounded => isGrounded;
    public Vector2 GroundNormal => groundNormal;
    public bool IsIntroJumping => isIntroJumping;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        initialScale = transform.localScale;
        baseSpawnX = transform.position.x;

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }
        }

        if (spriteRenderer != null && defaultSprite == null)
        {
            defaultSprite = spriteRenderer.sprite;
        }
    }

    private void OnEnable()
    {
        GameManager.OnGameStart += HandleGameStart;
        GameManager.OnGameOver += HandleGameOver;
    }

    private void OnDisable()
    {
        GameManager.OnGameStart -= HandleGameStart;
        GameManager.OnGameOver -= HandleGameOver;
        CameraFollow.OnCameraPanComplete -= OnCameraPanCompleteForIntro;
    }

    private void HandleGameStart()
    {
        if (enableJumpIntro)
        {
            if (CameraFollow.Instance != null)
            {
                CameraFollow.Instance.StartPanDownIfNeeded();
            }

            if (CameraFollow.Instance != null && CameraFollow.Instance.IsPanningDown)
            {
                CameraFollow.OnCameraPanComplete -= OnCameraPanCompleteForIntro;
                CameraFollow.OnCameraPanComplete += OnCameraPanCompleteForIntro;
            }
            else
            {
                TriggerJumpIntro();
            }
        }
    }

    private void OnCameraPanCompleteForIntro()
    {
        CameraFollow.OnCameraPanComplete -= OnCameraPanCompleteForIntro;
        if (enableJumpIntro)
        {
            TriggerJumpIntro();
        }
    }

    private void HandleGameOver()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.simulated = false;
        }

        // Hide sprite renderers on Player and child objects (e.g. Visuals)
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>();
        foreach (var sr in renderers)
        {
            sr.enabled = false;
        }

        enabled = false;
    }

    private void Start()
    {
        baseSpawnX = transform.position.x;
        if (GameManager.Instance != null && !GameManager.Instance.IsGameStarted)
        {
            if (enableJumpIntro)
            {
                isIntroJumping = true;
                currentJumpYOffset = startYOffset;
                currentJumpXOffset = startXOffset;
                currentJumpPitch = jumpPitchAngle;

                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;

                UpdateIntroPosition();
            }
        }
        else if (enableJumpIntro)
        {
            TriggerJumpIntro();
        }
    }

    private void OnDestroy()
    {
        jumpSequence?.Kill();
        transform.DOKill();
    }

    public void TriggerJumpIntro()
    {
        if (!enableJumpIntro) return;

        jumpSequence?.Kill();

        isIntroJumping = true;
        currentJumpYOffset = startYOffset;
        currentJumpXOffset = startXOffset;
        currentJumpPitch = jumpPitchAngle;

        PlayJumpDust();

        if (spriteRenderer != null && jumpSprite != null)
        {
            spriteRenderer.sprite = jumpSprite;
        }

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        UpdateIntroPosition();

        jumpSequence = DOTween.Sequence().SetLink(gameObject);

        if (jumpIntroDelay > 0f)
        {
            jumpSequence.AppendInterval(jumpIntroDelay);
        }

        float upDuration = jumpDuration * 0.45f;
        float downDuration = jumpDuration * 0.55f;

        jumpSequence.Append(
            DOVirtual.Float(startYOffset, jumpHeight, upDuration, y => currentJumpYOffset = y)
                .SetEase(jumpUpEase)
        );
        jumpSequence.Join(
            DOVirtual.Float(startXOffset, landingXOffset, jumpDuration, x => currentJumpXOffset = x)
                .SetEase(Ease.OutQuad)
        );
        jumpSequence.Join(
            DOVirtual.Float(jumpPitchAngle, -jumpPitchAngle, jumpDuration, pitch => currentJumpPitch = pitch)
                .SetEase(Ease.InOutSine)
        );
        jumpSequence.Append(
            DOVirtual.Float(jumpHeight, 0f, downDuration, y => currentJumpYOffset = y)
                .SetEase(jumpDownEase)
        );

        jumpSequence.OnComplete(() =>
        {
            OnIntroJumpLand();
        });
    }

    private void OnIntroJumpLand()
    {
        isIntroJumping = false;
        baseSpawnX += landingXOffset;
        currentJumpYOffset = 0f;
        currentJumpXOffset = 0f;
        currentJumpPitch = 0f;

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        TriggerJuice(landPunchScale);
        PlayLandDust();

        if (spriteRenderer != null && defaultSprite != null)
        {
            spriteRenderer.sprite = defaultSprite;
        }

        if (CameraFollow.Instance != null)
        {
            CameraFollow.Instance.ShakeCamera(0.2f, 0.35f);
        }
    }

    private void UpdateIntroPosition()
    {
        if (!isIntroJumping) return;

        float targetX = baseSpawnX + currentJumpXOffset;
        float groundY = transform.position.y;
        if (DynamicTerrainGenerator.Instance != null)
        {
            groundY = DynamicTerrainGenerator.Instance.CalculateHeightAt(targetX);
        }
        float targetY = groundY + currentJumpYOffset;

        rb.MovePosition(new Vector2(targetX, targetY));

        // Pitch & slope angle rotation during jump intro
        float slopeAngle = 0f;
        if (DynamicTerrainGenerator.Instance != null)
        {
            float delta = 0.5f;
            float yLeft = DynamicTerrainGenerator.Instance.CalculateHeightAt(targetX - delta);
            float yRight = DynamicTerrainGenerator.Instance.CalculateHeightAt(targetX + delta);
            Vector2 slopeTangent = new Vector2(delta * 2f, yRight - yLeft).normalized;
            slopeAngle = Mathf.Atan2(slopeTangent.y, slopeTangent.x) * Mathf.Rad2Deg;
        }

        float finalAngle = slopeAngle + currentJumpPitch;
        rb.MoveRotation(finalAngle);
    }

    private void TriggerJuice(Vector3 punchAmount)
    {
        if (!enableJuice) return;
        transform.DOKill();
        transform.localScale = initialScale;
        transform.DOPunchScale(punchAmount, juiceDuration, 6, 0.5f);
    }

    private void PlayJumpDust()
    {
        if (!enableDustParticles) return;
        Vector3 spawnPos = transform.position + feetOffset;
        if (jumpDustPrefab != null)
        {
            ParticleSystem ps = Instantiate(jumpDustPrefab, spawnPos, Quaternion.identity);
            ps.Play();
            Destroy(ps.gameObject, ps.main.duration + ps.main.startLifetime.constantMax);
        }
        else
        {
            DustParticleEffects.PlayJumpDust(spawnPos);
        }
    }

    private void PlayLandDust()
    {
        if (!enableDustParticles) return;
        Vector3 spawnPos = transform.position + feetOffset;
        if (landDustPrefab != null)
        {
            ParticleSystem ps = Instantiate(landDustPrefab, spawnPos, Quaternion.identity);
            ps.Play();
            Destroy(ps.gameObject, ps.main.duration + ps.main.startLifetime.constantMax);
        }
        else
        {
            DustParticleEffects.PlayLandDust(spawnPos);
        }
    }

    private void Update()
    {
        if (isIntroJumping)
        {
            if (spriteRenderer != null && jumpSprite != null)
            {
                spriteRenderer.sprite = jumpSprite;
            }
            return;
        }

        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;

        if (Input.GetButtonDown("Jump"))
        {
            jumpBufferTimer = jumpBufferTime;
        }
        else
        {
            jumpBufferTimer -= Time.deltaTime;
        }

        UpdateSprite();
    }

    private void UpdateSprite()
    {
        if (spriteRenderer == null) return;

        if (!isGrounded)
        {
            if (jumpSprite != null)
            {
                spriteRenderer.sprite = jumpSprite;
            }
        }
        else
        {
            float moveInput = Input.GetAxisRaw("Horizontal");
            float speed = rb != null ? Mathf.Abs(rb.linearVelocity.x) : 0f;
            bool isIdle = Mathf.Approximately(moveInput, 0f) && speed < 0.1f;

            if (isIdle && idleSprite != null)
            {
                spriteRenderer.sprite = idleSprite;
            }
            else if (defaultSprite != null)
            {
                spriteRenderer.sprite = defaultSprite;
            }
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
                PlayLandDust();
                if (spriteRenderer != null && defaultSprite != null)
                {
                    spriteRenderer.sprite = defaultSprite;
                }
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
        if (isIntroJumping)
        {
            UpdateIntroPosition();
            return;
        }

        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

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
            PlayJumpDust();

            if (spriteRenderer != null && jumpSprite != null)
            {
                spriteRenderer.sprite = jumpSprite;
            }

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

        // Draw Jump Intro Start and Landing Gizmos
        if (enableJumpIntro)
        {
            float targetBaseX = Application.isPlaying ? baseSpawnX : transform.position.x;
            float targetLandingX = targetBaseX + landingXOffset;
            float targetStartX = targetBaseX + startXOffset;

            Gizmos.color = Color.yellow;
            Vector3 landingGizmoPos = new Vector3(targetLandingX, transform.position.y, transform.position.z);
            if (DynamicTerrainGenerator.Instance != null)
            {
                landingGizmoPos.y = DynamicTerrainGenerator.Instance.CalculateHeightAt(targetLandingX);
            }
            Gizmos.DrawWireCube(landingGizmoPos, new Vector3(0.8f, 0.8f, 0f));

            Gizmos.color = Color.cyan;
            Vector3 startGizmoPos = new Vector3(targetStartX, landingGizmoPos.y + startYOffset, transform.position.z);
            Gizmos.DrawWireSphere(startGizmoPos, 0.3f);
            Gizmos.DrawLine(startGizmoPos, landingGizmoPos);
        }
    }
}
