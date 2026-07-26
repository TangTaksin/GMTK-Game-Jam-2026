using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(Collider2D))]
public class ChasingThreat : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private float baseSpeed = 3.5f;
    [SerializeField] private float maxDistanceBehind = 14f;
    [SerializeField] private float speedIncreasePerMeter = 0.01f;

    [Header("Collision & Activation Settings")]
    [SerializeField] private string playerTag = "Player";
    [Tooltip("Seconds after first player input before threat starts moving.")]
    [SerializeField] private float startGraceDelay = 1.0f;

    [Header("Terrain & Slope Settings")]
    [SerializeField] private bool followTerrainCurve = true;
    [SerializeField] private float yOffsetFromGround = 0f;
    [Tooltip("Enable automatic slope rotation to tilt threat along terrain curves.")]
    [SerializeField] private bool autoSlopeRotation = true;
    [Tooltip("Rotation smoothing speed (higher = faster response).")]
    [SerializeField] private float rotationSpeed = 10f;
    [Tooltip("Maximum allowed tilt angle in degrees.")]
    [SerializeField] private float maxTiltAngle = 60f;
    [Tooltip("Distance delta to sample terrain slope.")]
    [SerializeField] private float slopeSampleDistance = 0.5f;

    [Header("Squash & Stretch Settings")]
    [Tooltip("Enable smooth pulse scaling (expanding/contracting) during movement.")]
    [SerializeField] private bool enableSquashAndStretch = true;
    [SerializeField] private float pulseSpeed = 6.0f;
    [SerializeField] private float pulseAmount = 0.05f;

    [Header("Jump Intro Settings")]
    [Tooltip("Enable jump intro animation when threat appears.")]
    [SerializeField] private bool enableJumpIntro = true;
    [Tooltip("Delay in seconds before starting the jump intro animation.")]
    [SerializeField] private float jumpIntroDelay = 0.3f;
    [Tooltip("Peak jump height above ground surface.")]
    [SerializeField] private float jumpHeight = 3.5f;
    [Tooltip("Initial Y offset below ground surface before jump.")]
    [SerializeField] private float startYOffset = -5.0f;
    [Tooltip("Initial X offset relative to starting position.")]
    [SerializeField] private float startXOffset = -4.0f;
    [Tooltip("Landing X offset relative to starting position (0 = land on spawn position).")]
    [SerializeField] private float landingXOffset = 0.0f;
    [Tooltip("Total duration of jump animation in seconds.")]
    [SerializeField] private float jumpDuration = 0.85f;
    [Tooltip("Rotation pitch effect during jump (degrees).")]
    [SerializeField] private float jumpPitchAngle = 20f;
    [SerializeField] private Ease jumpUpEase = Ease.OutQuad;
    [SerializeField] private Ease jumpDownEase = Ease.InQuad;

    [Header("Sorting Layer Settings")]
    [Tooltip("Sorting layer name for threat sprite renderer.")]
    [SerializeField] private string threatSortingLayer = "Default";
    [Tooltip("Sorting order for threat sprite renderer (set to 10 or higher to render in front of ground fill).")]
    [SerializeField] private int threatSortingOrder = 10;

    [Header("VFX / Hit Settings")]
    [SerializeField] private ParticleSystem bloodParticlePrefab;
    [SerializeField] private ParticleSystem landDustPrefab;

    [Header("Game Over Retreat Settings")]
    [Tooltip("Enable retreat (turn Y 180° and move left) when game over occurs.")]
    [SerializeField] private bool retreatOnGameOver = true;
    [Tooltip("Speed when retreating backwards to the left after game over.")]
    [SerializeField] private float retreatSpeed = 6.0f;
    [Tooltip("Duration of 180° turn animation in seconds.")]
    [SerializeField] private float retreatTurnDuration = 0.35f;
    [Tooltip("Use 2D SpriteRenderer flipX instead of 3D Transform Y-rotation to prevent pivot warping.")]
    [SerializeField] private bool useSpriteFlipX = true;

    // Component Cache
    private Rigidbody2D rb;
    private BoxCollider2D boxCollider;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D playerRb;

    // State Variables
    private bool isThreatActive;
    private bool isJumping;
    private bool isRetreating;
    private float basePositionX;
    private float currentJumpYOffset;
    private float currentJumpXOffset;
    private float currentJumpPitch;
    private float currentYRotation;
    private float stunTimer = 0f;
    private Vector3 initialScale = Vector3.one;

    // Tweens
    private Tween squashTween;
    private Tween graceTween;
    private Tween turnTween;
    private Sequence jumpSequence;

    public static ChasingThreat Instance { get; private set; }

    public bool IsJumping => isJumping;
    public bool IsRetreating => isRetreating;
    public bool IsThreatActive => isThreatActive;
    public bool IsStunned => stunTimer > 0f;

    public void Initialize(float speed, float speedIncrease, float maxDistance, float graceDelay)
    {
        baseSpeed = speed;
        speedIncreasePerMeter = speedIncrease;
        maxDistanceBehind = maxDistance;
        startGraceDelay = graceDelay;
        basePositionX = transform.position.x;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // Update reference if needed
            Instance = this;
        }
        else
        {
            Instance = this;
        }

        rb = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        initialScale = transform.localScale;
        basePositionX = transform.position.x;

        if (spriteRenderer != null)
        {
            spriteRenderer.sortingLayerName = threatSortingLayer;
            spriteRenderer.sortingOrder = threatSortingOrder;
        }

        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
        }
    }

    private void OnEnable()
    {
        GameManager.OnGameOver += HandleGameOver;
        GameManager.OnGameStart += HandleGameStart;
    }

    private void OnDisable()
    {
        GameManager.OnGameOver -= HandleGameOver;
        GameManager.OnGameStart -= HandleGameStart;
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
        else
        {
            SnapToGround();
        }
    }

    private void OnCameraPanCompleteForIntro()
    {
        CameraFollow.OnCameraPanComplete -= OnCameraPanCompleteForIntro;
        if (enableJumpIntro)
        {
            TriggerJumpIntro();
        }
        else
        {
            SnapToGround();
        }
    }

    private void Start()
    {
        basePositionX = transform.position.x;
        FindPlayer();

        if (GameManager.Instance != null && !GameManager.Instance.IsGameStarted)
        {
            if (enableJumpIntro)
            {
                isJumping = true;
                currentJumpYOffset = startYOffset;
                currentJumpXOffset = startXOffset;
                currentJumpPitch = jumpPitchAngle;

                if (rb != null)
                {
                    rb.bodyType = RigidbodyType2D.Kinematic;
                    rb.linearVelocity = Vector2.zero;
                    rb.angularVelocity = 0f;
                }

                SnapToGround();
            }
        }
        else
        {
            if (enableJumpIntro)
            {
                TriggerJumpIntro();
            }
            else
            {
                SnapToGround();
            }
        }
    }

    private void OnDestroy()
    {
        KillAllTweens();
    }

    private void FindPlayer()
    {
        if (playerTransform != null)
        {
            playerRb = playerTransform.GetComponent<Rigidbody2D>();
            return;
        }

        GameObject playerObj = GameObject.FindWithTag(playerTag);
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            playerRb = playerObj.GetComponent<Rigidbody2D>();
        }
    }

    private void HandleGameOver()
    {
        if (retreatOnGameOver)
        {
            TriggerGameOverRetreat();
        }
    }

    public void TriggerGameOverRetreat()
    {
        if (isRetreating) return;

        // Synchronize basePositionX to current actual position to eliminate position warping
        basePositionX = transform.position.x;
        isRetreating = true;

        jumpSequence?.Kill();
        squashTween?.Kill();
        isJumping = false;
        currentJumpYOffset = 0f;
        currentJumpXOffset = 0f;
        currentJumpPitch = 0f;

        turnTween?.Kill();
        turnTween = DOVirtual.Float(currentYRotation, 180f, retreatTurnDuration, y =>
        {
            currentYRotation = y;
            if (useSpriteFlipX && spriteRenderer != null)
            {
                spriteRenderer.flipX = (y >= 90f);
            }
        })
        .SetEase(Ease.OutQuad)
        .SetLink(gameObject);
    }

    public void TriggerJumpIntro()
    {
        if (!enableJumpIntro) return;

        jumpSequence?.Kill();
        graceTween?.Kill();
        isThreatActive = false;

        isJumping = true;
        currentJumpYOffset = startYOffset;
        currentJumpXOffset = startXOffset;
        currentJumpPitch = jumpPitchAngle;

        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        SnapToGround();

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
            isJumping = false;
            basePositionX += landingXOffset;
            currentJumpYOffset = 0f;
            currentJumpXOffset = 0f;
            currentJumpPitch = 0f;
            OnJumpLand();
        });
    }

    private void PlayLandDust()
    {
        Vector3 spawnPos = transform.position + new Vector3(0f, -0.5f, 0f);
        if (landDustPrefab == null)
        {
#if UNITY_EDITOR
            landDustPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<ParticleSystem>("Assets/_Project/Prefabs/LandDustParticle.prefab");
#endif
        }

        if (landDustPrefab != null)
        {
            ParticleSystem ps = Instantiate(landDustPrefab, spawnPos, Quaternion.identity);
            ps.gameObject.SetActive(true);
            ps.Play();
            Destroy(ps.gameObject, 2.0f);
        }
        else
        {
            DustParticleEffects.PlayLandDust(spawnPos);
        }
    }

    private void OnJumpLand()
    {
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        PlayLandDust();
        if (CameraFollow.Instance != null)
        {
            CameraFollow.Instance.ShakeCamera(0.2f, 0.35f);
        }

        ActivateThreat();

        if (enableSquashAndStretch)
        {
            squashTween?.Kill();
            transform.localScale = initialScale;

            squashTween = transform.DOPunchScale(new Vector3(0.25f, -0.35f, 0f), 0.35f, 6, 0.5f)
                .OnComplete(() =>
                {
                    squashTween = null;
                    if (isThreatActive) StartPulseAnimation();
                })
                .SetLink(gameObject);
        }
    }

    private void ActivateThreat()
    {
        if (isThreatActive || isJumping) return;
        if (GameManager.Instance != null && !GameManager.Instance.IsGameStarted) return;
        isThreatActive = true;
        StartPulseAnimation();
    }

    private void StartPulseAnimation()
    {
        if (!enableSquashAndStretch || squashTween != null) return;

        Vector3 targetScale = new Vector3(
            initialScale.x * (1f + pulseAmount),
            initialScale.y * (1f - pulseAmount * 0.5f),
            initialScale.z
        );

        float halfPeriod = pulseSpeed > 0f ? (Mathf.PI / pulseSpeed) : 0.25f;

        squashTween = transform.DOScale(targetScale, halfPeriod)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetLink(gameObject);
    }

    private void SnapToGround()
    {
        float targetX = basePositionX + (isJumping ? currentJumpXOffset : 0f);
        float yOffset = isJumping ? currentJumpYOffset : 0f;
        MoveToPosition(targetX, yOffset);
    }

    private float CalculateTargetY(float currentX)
    {
        if (DynamicTerrainGenerator.Instance == null) return transform.position.y;

        float bottomOffset;

        if (boxCollider != null)
        {
            float localBottom = boxCollider.offset.y - (boxCollider.size.y / 2f);
            bottomOffset = localBottom * transform.localScale.y;
        }
        else if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            float spriteHeight = spriteRenderer.sprite.rect.height / spriteRenderer.sprite.pixelsPerUnit;
            float localBottom = -spriteHeight * 0.5f;
            bottomOffset = localBottom * transform.localScale.y;
        }
        else
        {
            bottomOffset = -0.5f * transform.localScale.y;
        }

        float groundY = DynamicTerrainGenerator.Instance.CalculateHeightAt(currentX);
        return groundY - bottomOffset + yOffsetFromGround;
    }

    private void MoveToPosition(float targetX, float extraYOffset = 0f)
    {
        float targetY = followTerrainCurve ? CalculateTargetY(targetX) : transform.position.y;
        Vector2 nextPos = new Vector2(targetX, targetY + extraYOffset);

        if (rb != null)
        {
            rb.MovePosition(nextPos);
        }
        else
        {
            transform.position = nextPos;
        }

        UpdateSlopeRotation(targetX);
    }

    private void UpdateSlopeRotation(float currentX)
    {
        if (!autoSlopeRotation || DynamicTerrainGenerator.Instance == null) return;

        float delta = Mathf.Max(0.1f, slopeSampleDistance);
        float yLeft = DynamicTerrainGenerator.Instance.CalculateHeightAt(currentX - delta);
        float yRight = DynamicTerrainGenerator.Instance.CalculateHeightAt(currentX + delta);

        Vector2 slopeTangent = new Vector2(delta * 2f, yRight - yLeft).normalized;
        float targetAngle = Mathf.Atan2(slopeTangent.y, slopeTangent.x) * Mathf.Rad2Deg;

        if (isJumping)
        {
            targetAngle += currentJumpPitch;
        }

        targetAngle = Mathf.Clamp(targetAngle, -maxTiltAngle, maxTiltAngle);

        bool isFlipped = useSpriteFlipX ? (spriteRenderer != null && spriteRenderer.flipX) : (currentYRotation > 90f);
        float targetYRot = useSpriteFlipX ? 0f : (isRetreating ? currentYRotation : 0f);
        float finalZAngle = isFlipped ? -targetAngle : targetAngle;

        if (rb != null && rb.bodyType != RigidbodyType2D.Kinematic)
        {
            float currentZAngle = rb.rotation;
            float smoothZAngle = Mathf.LerpAngle(currentZAngle, finalZAngle, Time.fixedDeltaTime * rotationSpeed);
            rb.MoveRotation(smoothZAngle);

            if (!useSpriteFlipX)
            {
                Vector3 euler = transform.eulerAngles;
                euler.y = targetYRot;
                transform.eulerAngles = euler;
            }
        }
        else
        {
            float currentZAngle = transform.eulerAngles.z;
            float smoothZAngle = Mathf.LerpAngle(currentZAngle, finalZAngle, Time.fixedDeltaTime * rotationSpeed);
            transform.rotation = Quaternion.Euler(0f, targetYRot, smoothZAngle);
        }
    }

    private void FixedUpdate()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
        {
            if (retreatOnGameOver && !isRetreating)
            {
                TriggerGameOverRetreat();
            }

            if (!isRetreating) return;
        }

        // 1. Handle Game Over Retreat Movement
        if (isRetreating)
        {
            basePositionX -= retreatSpeed * Time.fixedDeltaTime;
            MoveToPosition(basePositionX);
            return;
        }

        // 2. Handle Stun from Super Bomb Blast
        if (stunTimer > 0f)
        {
            stunTimer -= Time.fixedDeltaTime;
            SnapToGround();
            return;
        }

        // 2. Handle Idle / Grace Period before Threat Activation
        if (!isThreatActive)
        {
            SnapToGround();

            if (isJumping || (GameManager.Instance != null && !GameManager.Instance.IsGameStarted))
            {
                return;
            }

            if (playerTransform == null) FindPlayer();
            if (playerTransform == null) return;

            bool hasPlayerInput = Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.01f || Input.GetButton("Jump");
            bool isPlayerMoving = playerRb != null && playerRb.linearVelocity.magnitude > 0.1f;

            if (hasPlayerInput || isPlayerMoving)
            {
                if (graceTween == null)
                {
                    if (startGraceDelay <= 0f)
                    {
                        ActivateThreat();
                    }
                    else
                    {
                        graceTween = DOVirtual.DelayedCall(startGraceDelay, ActivateThreat).SetLink(gameObject);
                    }
                }
            }
            return;
        }

        // 3. Active Pursuit Movement
        if (playerTransform == null) FindPlayer();
        if (playerTransform == null) return;

        float playerX = playerTransform.position.x;
        float extraSpeed = Mathf.Max(0f, playerX * speedIncreasePerMeter);
        float targetSpeed = baseSpeed + extraSpeed;

        float distanceBehind = playerX - basePositionX;
        if (distanceBehind > maxDistanceBehind)
        {
            float catchUpSpeed = targetSpeed + (distanceBehind - maxDistanceBehind) * 2f;
            targetSpeed = catchUpSpeed;
        }

        basePositionX += targetSpeed * Time.fixedDeltaTime;

        float targetX = basePositionX + (isJumping ? currentJumpXOffset : 0f);
        float yOffset = isJumping ? currentJumpYOffset : 0f;
        MoveToPosition(targetX, yOffset);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        CheckPlayerCollision(collision.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        CheckPlayerCollision(collision.gameObject);
    }

    private void CheckPlayerCollision(GameObject target)
    {
        if (GameManager.Instance != null && (GameManager.Instance.IsGameOver || GameManager.Instance.IsVictory)) return;

        // Ignore player collision if permanent bomb trap is placed
        PlayerTimer pt = target.GetComponent<PlayerTimer>();
        if (pt == null) pt = target.GetComponentInChildren<PlayerTimer>();
        if (pt != null && pt.IsPermanentTrapPlaced) return;

        if (target.CompareTag(playerTag) || target.GetComponent<PlayerMovement>() != null)
        {
            Debug.Log("<color=red>[ChasingThreat] Player caught by threat!</color>");

            Vector3 hitPos = target.transform.position;

            if (bloodParticlePrefab == null)
            {
#if UNITY_EDITOR
                bloodParticlePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<ParticleSystem>("Assets/_Project/Prefabs/BloodParticle.prefab");
#endif
            }

            if (bloodParticlePrefab != null)
            {
                ParticleSystem b = Instantiate(bloodParticlePrefab, hitPos, Quaternion.identity);
                b.gameObject.SetActive(true);
                b.Play();
                Destroy(b.gameObject, 2.0f);
            }
            else
            {
                DustParticleEffects.PlayBloodSplatter(hitPos);
            }

            PlayerTimer playerTimer = target.GetComponent<PlayerTimer>();
            if (playerTimer != null)
            {
                playerTimer.Explode();
            }
            else
            {
                if (CameraFollow.Instance != null) CameraFollow.Instance.ShakeCamera(0.4f, 0.7f);
                GameManager.Instance.TriggerGameOver();
            }

            if (retreatOnGameOver)
            {
                TriggerGameOverRetreat();
            }
        }
    }

    /// <summary>
    /// Knocks back and stuns ChasingThreat when player detonates the 999m Super Bomb!
    /// </summary>
    public void TakeBombBlast(float knockbackDistance = 35f, float stunDuration = 5.0f)
    {
        basePositionX -= knockbackDistance;
        stunTimer = stunDuration;

        if (CameraFollow.Instance != null)
        {
            CameraFollow.Instance.ShakeCamera(0.6f, 0.7f);
        }

        transform.DOKill();
        transform.DOPunchScale(new Vector3(0.5f, -0.5f, 0f), 0.5f, 8, 0.5f);

        if (spriteRenderer != null)
        {
            spriteRenderer.DOKill();
            spriteRenderer.color = Color.red;
            spriteRenderer.DOColor(Color.white, 0.8f);
        }

        Vector3 hitPos = transform.position;
        DustParticleEffects.PlayExplosion(hitPos);
        DustParticleEffects.PlayBloodSplatter(hitPos);

        SnapToGround();
    }

    private void KillAllTweens()
    {
        squashTween?.Kill();
        graceTween?.Kill();
        jumpSequence?.Kill();
        turnTween?.Kill();
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 pos = transform.position;

        // 1. Draw Threat Position Anchor
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(pos, 0.3f);

        if (DynamicTerrainGenerator.Instance != null)
        {
            float groundY = DynamicTerrainGenerator.Instance.CalculateHeightAt(pos.x);
            Vector3 groundPoint = new Vector3(pos.x, groundY, pos.z);

            // 2. Draw Line from Threat Position to Ground Surface
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(pos, groundPoint);
            Gizmos.DrawWireSphere(groundPoint, 0.25f);

            // 3. Draw Slope Tangent Line along Terrain
            if (autoSlopeRotation)
            {
                float delta = Mathf.Max(0.1f, slopeSampleDistance);
                float yLeft = DynamicTerrainGenerator.Instance.CalculateHeightAt(pos.x - delta);
                float yRight = DynamicTerrainGenerator.Instance.CalculateHeightAt(pos.x + delta);

                Vector3 pLeft = new Vector3(pos.x - delta, yLeft, pos.z);
                Vector3 pRight = new Vector3(pos.x + delta, yRight, pos.z);

                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(pLeft, pRight);
                Gizmos.DrawSphere(pLeft, 0.15f);
                Gizmos.DrawSphere(pRight, 0.15f);
            }
        }

        // 4. Draw Catch-Up Threshold Line relative to Player
        if (playerTransform != null)
        {
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.8f);
            float maxBehindX = playerTransform.position.x - maxDistanceBehind;
            Gizmos.DrawLine(new Vector3(maxBehindX, pos.y - 8f, pos.z), new Vector3(maxBehindX, pos.y + 8f, pos.z));
        }

        // 5. Draw Jump Start and Landing Preview Gizmos
        if (enableJumpIntro)
        {
            float targetBaseX = Application.isPlaying ? basePositionX : pos.x;
            float targetLandingX = targetBaseX + landingXOffset;
            float targetStartX = targetBaseX + startXOffset;

            // Draw Landing Target on Terrain Surface (Magenta marker)
            Gizmos.color = Color.magenta;
            Vector3 landingGizmoPos = new Vector3(targetLandingX, pos.y, pos.z);
            if (DynamicTerrainGenerator.Instance != null)
            {
                landingGizmoPos.y = DynamicTerrainGenerator.Instance.CalculateHeightAt(targetLandingX);
            }
            Gizmos.DrawWireCube(landingGizmoPos, new Vector3(1f, 1f, 0f));
            Gizmos.DrawSphere(landingGizmoPos, 0.25f);

            // Draw Jump Arc Start Point (Cyan marker)
            Gizmos.color = Color.cyan;
            Vector3 startGizmoPos = new Vector3(targetStartX, landingGizmoPos.y + startYOffset, pos.z);
            Gizmos.DrawWireSphere(startGizmoPos, 0.3f);
            Gizmos.DrawLine(startGizmoPos, landingGizmoPos);
        }
    }
}
