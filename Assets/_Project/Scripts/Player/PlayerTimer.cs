using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerTimer : MonoBehaviour
{
    [Header("Bomb Timer Settings")]
    [SerializeField] private int maxTime = 5;
    [SerializeField] private float currentTime;
    [SerializeField] private float resetGroundDuration = 3.0f; // 3 seconds on ground to reset

    [Header("Carrying & Placement")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Vector3 carryOffset = new Vector3(0f, 1.2f, 0f);
    [SerializeField] private Vector3 dropOffset = new Vector3(1.2f, 0f, 0f); // Position in front of player
    [SerializeField] private float yOffsetFromGround = 0.05f; // Extra ground height buffer
    [SerializeField] private bool startHeld = true;

    [Header("Defuse Animation Settings")]
    [SerializeField] private bool enableProceduralDefuseAnim = true;
    [SerializeField] private bool enableScaleSquash = false;
    [SerializeField] private float defuseSquashY = 1.0f;
    [SerializeField] private float defuseStretchX = 1.0f;
    [SerializeField] private float defuseWiggleSpeed = 25.0f;
    [SerializeField] private float defuseWiggleAngle = 4.0f;
    [Tooltip("Downward vertical offset applied while defusing to keep feet grounded.")]
    [SerializeField] private float defuseYOffset = 0.0f;

    [Header("Explosion & Audio Settings")]
    [SerializeField] private ParticleSystem explosionParticlePrefab;
    [Tooltip("Playback pitch/speed for BombFuse sound (0.75 = 25% slower).")]
    [SerializeField] private float fuseAudioPitch = 0.75f;
    [Tooltip("Time threshold in seconds when warning ticks start (default 6s).")]
    [SerializeField] private int warningTimeThreshold = 6;

    [Header("Ground Check")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckDistance = 0.8f;

    private Rigidbody2D rb;
    private PlayerMovement playerMovement;
    private bool isHeld;
    private bool isGrounded;
    private float groundRestTimer = 0f;
    private bool isExploded;
    private bool hasDefusedThisDrop = false;
    private int lastWarningTick = -1;

    private Vector3 initialVisualScale = Vector3.one;
    private Vector3 initialVisualLocalPos = Vector3.zero;
    private bool visualScaleCached = false;
    private Transform targetVisualTransform;
    private bool wasDefusing = false;

    private bool isSuperBombReady = false;
    private bool isPermanentTrapPlaced = false;

    public float CurrentTime => currentTime;
    public int CurrentTimeDisplay => Mathf.CeilToInt(currentTime);
    public int MaxTime => maxTime;
    public bool IsHeld => isHeld;
    public bool IsGrounded => isGrounded;
    public float GroundRestTimer => groundRestTimer;
    public float ResetGroundDuration => resetGroundDuration;
    public bool IsDefusing => !isHeld && isGrounded && !hasDefusedThisDrop && currentTime < (maxTime - 0.05f) && groundRestTimer < resetGroundDuration;
    public bool IsSuperBombReady => isSuperBombReady;
    public bool IsPermanentTrapPlaced => isPermanentTrapPlaced;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        currentTime = maxTime;
        isHeld = startHeld;

        FindPlayer();
    }

    private void OnEnable()
    {
        GameManager.OnGameOver += HandleGameOver;
        ScoreManager.OnDistance999Reached += EnableSuperBomb;
    }

    private void OnDisable()
    {
        GameManager.OnGameOver -= HandleGameOver;
        ScoreManager.OnDistance999Reached -= EnableSuperBomb;
        if (AudioManager.Instance != null) AudioManager.Instance.StopLoopingSFX();
    }

    private void EnableSuperBomb()
    {
        if (isPermanentTrapPlaced) return;
        isSuperBombReady = true;
        currentTime = maxTime;
        Debug.Log("<color=gold>Press [S]</color>");

        // 🔊 เล่นเสียง BombDefuse เมื่อ Super Bomb พร้อมใช้งานที่ 999m
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopLoopingSFX();
            AudioManager.Instance.PlaySFX("BombDefuse");
        }
    }

    public void LaunchSuperBomb()
    {
        if (!isSuperBombReady || isPermanentTrapPlaced) return;

        // Cannot deploy Super Bomb in mid-air or during intro jump
        if (playerMovement != null && (!playerMovement.IsGrounded || playerMovement.IsIntroJumping)) return;

        isSuperBombReady = false;
        isPermanentTrapPlaced = true;

        // Position bomb trap to the LEFT of player (behind player towards approaching monster)
        isHeld = false;
        if (playerTransform != null)
        {
            float trapX = playerTransform.position.x - 3.5f;
            float trapY = GetTerrainSurfaceY(trapX);
            transform.position = new Vector3(trapX, trapY, playerTransform.position.z);
        }

        if (rb != null)
        {
            rb.simulated = true;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        // Enable PermanentSuperBomb component on this existing bomb
        PermanentSuperBomb trap = GetComponent<PermanentSuperBomb>();
        if (trap == null)
        {
            trap = gameObject.AddComponent<PermanentSuperBomb>();
        }
        trap.enabled = true;

        // Play drop dust & flash juice
        DustParticleEffects.PlayLandDust(transform.position);

        // 🔊 เล่นเสียง PlayerLand เมื่อกดปุ่ม S วางกับดักระเบิดลงพื้น
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("PlayerLand");
        }

        // Freeze Player input & movement, waiting for ChasingThreat to run up and hit the bomb!
        if (playerMovement != null)
        {
            playerMovement.FreezeForTrapWait();
        }
        else if (playerTransform != null)
        {
            PlayerMovement pm = playerTransform.GetComponent<PlayerMovement>();
            if (pm != null) pm.FreezeForTrapWait();
        }

        // Keep current time at max so bomb stays stable waiting for monster
        currentTime = maxTime;
    }

    private void HandleGameOver()
    {
        isExploded = true;
        gameObject.SetActive(false);
    }

    private void Start()
    {
        FindPlayer();
    }

    private void FindPlayer()
    {
        if (playerTransform != null)
        {
            playerMovement = playerTransform.GetComponent<PlayerMovement>();
            return;
        }

        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            playerMovement = player.GetComponent<PlayerMovement>();
        }
    }

    private void Update()
    {
        if (isExploded) return;
        if (playerTransform == null) FindPlayer();

        // Check if player has explicit movement input
        float moveX = Input.GetAxisRaw("Horizontal");
        if (moveX < 0f) moveX = 0f; // Ignore left input
        bool hasInput = moveX > 0.05f || Input.GetButton("Jump");

        bool isPlayerGrounded = playerMovement != null ? playerMovement.IsGrounded : true;
        bool isAirborne = !isPlayerGrounded;
        bool isIntroJumping = playerMovement != null && playerMovement.IsIntroJumping;

        // Check for Super Bomb Trigger Input (S key or Down Arrow) - MUST BE GROUNDED!
        if (isSuperBombReady && isPlayerGrounded && !isIntroJumping && (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)))
        {
            LaunchSuperBomb();
        }

        // Check player speed & forcefully lock velocity to zero if no input on ground and speed < 0.5f
        Rigidbody2D playerRb = (playerTransform != null) ? playerTransform.GetComponent<Rigidbody2D>() : null;
        float playerSpeed = (playerRb != null) ? playerRb.linearVelocity.magnitude : 0f;

        if (!hasInput && isPlayerGrounded && playerSpeed < 0.5f && playerRb != null)
        {
            playerRb.linearVelocity = Vector2.zero;
            playerSpeed = 0f;
        }

        bool isPlayerTrulyStill = !hasInput && isPlayerGrounded && playerSpeed <= 0.05f;

        // Player MUST hold bomb if not truly still, airborne (jumping/falling), or in intro jump
        bool shouldHoldBomb = !isPlayerTrulyStill || isAirborne || isIntroJumping;

        // If bomb was placed as a permanent trap, freeze timer and stay on ground!
        if (isPermanentTrapPlaced)
        {
            currentTime = maxTime;
            CheckGrounded();
            UpdateDefuseAnimation();

            // 🔊 Handle Bomb Fuse sound loop for Permanent Super Bomb Trap
            if (AudioManager.Instance != null && GameManager.Instance != null && GameManager.Instance.IsGameStarted && !isExploded)
            {
                AudioManager.Instance.PlayLoopingSFX("BombFuse", fuseAudioPitch);
            }

            return;
        }

        // If Super Bomb is ready, freeze timer countdown at maxTime!
        if (isSuperBombReady)
        {
            currentTime = maxTime;
        }

        // Auto PickUp when moving, Auto Drop when stopped
        if (shouldHoldBomb)
        {
            if (!isHeld)
            {
                PickUpBomb();
            }

            FollowPlayerHead();
            groundRestTimer = 0f;

            // Timer counts down while carrying and moving
            if (!isIntroJumping && !isSuperBombReady)
            {
                currentTime -= Time.deltaTime;
            }
        }
        else
        {
            if (isHeld)
            {
                DropBomb();
            }

            CheckGrounded();

            if (isGrounded)
            {
                if (!hasDefusedThisDrop && currentTime < maxTime - 0.05f)
                {
                    // Count up rest timer to 3 seconds on ground to reset bomb time
                    groundRestTimer += Time.deltaTime;

                    if (groundRestTimer >= resetGroundDuration)
                    {
                        currentTime = maxTime;
                        groundRestTimer = 0f;
                        hasDefusedThisDrop = true; // Stop defuse animation and forbid counting defuse rest timer again

                        // 🔊 เล่นเสียงกู้ระเบิดสำเร็จ BombDefuse
                        if (AudioManager.Instance != null)
                        {
                            AudioManager.Instance.StopLoopingSFX();
                            AudioManager.Instance.PlaySFX("BombDefuse");
                        }
                    }
                }
                else
                {
                    // Defuse complete or full: LOCK currentTime at maxTime until player presses move input again!
                    currentTime = maxTime;
                    groundRestTimer = 0f;
                }
            }
            else
            {
                groundRestTimer = 0f;
            }
        }

        // Handle Bomb Fuse & Defusing sound loops (includes active fuse while Permanent Trap is waiting for ChasingThreat)
        bool isFuseActive = (shouldHoldBomb || isPermanentTrapPlaced) && !isIntroJumping && !isSuperBombReady && !isExploded && (GameManager.Instance != null && GameManager.Instance.IsGameStarted);
        bool isDefusingActive = IsDefusing && !isExploded && (GameManager.Instance != null && GameManager.Instance.IsGameStarted);

        if (isFuseActive)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayLoopingSFX("BombFuse", fuseAudioPitch);
        }
        else if (isDefusingActive)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayLoopingSFX("Defusing");
        }
        else
        {
            if (AudioManager.Instance != null) AudioManager.Instance.StopLoopingSFX();
        }

        // Handle Bomb Warning sound ticks at warningTimeThreshold (6s default)
        int currentCeilTime = Mathf.CeilToInt(currentTime);
        if (currentCeilTime <= warningTimeThreshold && currentCeilTime > 0 && isFuseActive)
        {
            if (currentCeilTime != lastWarningTick)
            {
                lastWarningTick = currentCeilTime;
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySFX("BombWarning");
                }
            }
        }
        else if (currentCeilTime > warningTimeThreshold || !isFuseActive)
        {
            lastWarningTick = -1;
        }

        UpdateDefuseAnimation();

        if (currentTime <= 0f)
        {
            Explode();
        }
    }

    /// <summary>
    /// Deducts time from bomb timer when hit by an obstacle.
    /// </summary>
    public void DeductTime(float amount)
    {
        if (isExploded || isSuperBombReady || isPermanentTrapPlaced) return;
        currentTime = Mathf.Max(0f, currentTime - amount);
    }

    private void UpdateDefuseAnimation()
    {
        if (playerTransform == null) return;

        if (targetVisualTransform == null)
        {
            Transform v = playerTransform.Find("Visuals");
            if (v == null)
            {
                SpriteRenderer sr = playerTransform.GetComponentInChildren<SpriteRenderer>();
                if (sr != null) v = sr.transform;
            }
            targetVisualTransform = v != null ? v : playerTransform;
        }

        if (!visualScaleCached)
        {
            initialVisualScale = targetVisualTransform.localScale;
            initialVisualLocalPos = targetVisualTransform.localPosition;
            visualScaleCached = true;
        }

        bool isDefusingNow = IsDefusing;

        if (enableProceduralDefuseAnim)
        {
            if (isDefusingNow)
            {
                wasDefusing = true;

                if (enableScaleSquash)
                {
                    Vector3 defuseScale = new Vector3(
                        initialVisualScale.x * defuseStretchX,
                        initialVisualScale.y * defuseSquashY,
                        initialVisualScale.z
                    );
                    Vector3 defuseLocalPos = initialVisualLocalPos + new Vector3(0f, defuseYOffset, 0f);
                    targetVisualTransform.localScale = defuseScale;
                    targetVisualTransform.localPosition = defuseLocalPos;
                }

                // Intense fast-hands defuse wiggle animation
                float wiggleZ = Mathf.Sin(Time.time * defuseWiggleSpeed) * defuseWiggleAngle;
                targetVisualTransform.localRotation = Quaternion.Euler(0f, 0f, wiggleZ);
            }
            else if (wasDefusing)
            {
                wasDefusing = false;
                if (enableScaleSquash)
                {
                    targetVisualTransform.localScale = initialVisualScale;
                    targetVisualTransform.localPosition = initialVisualLocalPos;
                }
                targetVisualTransform.localRotation = Quaternion.identity;
            }
        }
    }

    private void FollowPlayerHead()
    {
        if (playerTransform == null) return;

        Vector3 targetPos = playerTransform.position + carryOffset;
        transform.position = targetPos;
        transform.rotation = Quaternion.identity;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.simulated = false; // Disable physics while held on head
        }
    }

    public float GetTerrainSurfaceY(float currentX)
    {
        if (DynamicTerrainGenerator.Instance == null) return transform.position.y;

        float bottomOffset;
        Collider2D col = GetComponent<Collider2D>();
        if (col is CircleCollider2D circle)
        {
            float localBottom = circle.offset.y - circle.radius;
            bottomOffset = localBottom * transform.localScale.y;
        }
        else if (col is BoxCollider2D box)
        {
            float localBottom = box.offset.y - (box.size.y * 0.5f);
            bottomOffset = localBottom * transform.localScale.y;
        }
        else
        {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
            {
                float spriteHeight = sr.sprite.rect.height / sr.sprite.pixelsPerUnit;
                float localBottom = -spriteHeight * 0.5f;
                bottomOffset = localBottom * transform.localScale.y;
            }
            else
            {
                bottomOffset = -0.5f * transform.localScale.y;
            }
        }

        float groundY = DynamicTerrainGenerator.Instance.CalculateHeightAt(currentX);
        return groundY - bottomOffset + yOffsetFromGround;
    }

    public void DropBomb()
    {
        isHeld = false;
        groundRestTimer = 0f;
        hasDefusedThisDrop = false;

        if (playerTransform != null)
        {
            float targetX = playerTransform.position.x + dropOffset.x;
            float targetY = GetTerrainSurfaceY(targetX);
            transform.position = new Vector3(targetX, targetY, playerTransform.position.z);
        }

        if (rb != null)
        {
            rb.simulated = true; // Enable physics to drop to ground
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    public void PickUpBomb()
    {
        isHeld = true;
        groundRestTimer = 0f;
        hasDefusedThisDrop = false;
        if (rb != null)
        {
            rb.simulated = false;
        }
    }

    private void CheckGrounded()
    {
        Vector2 rayOrigin = transform.position;
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            rayOrigin = col.bounds.center;
        }

        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.down, groundCheckDistance, groundLayer);
        isGrounded = hit.collider != null;

        // Supplemental terrain height check so bomb never misses ground state
        if (!isGrounded && DynamicTerrainGenerator.Instance != null)
        {
            float surfaceY = GetTerrainSurfaceY(transform.position.x);
            if (Mathf.Abs(transform.position.y - surfaceY) <= 0.35f)
            {
                isGrounded = true;
            }
        }
    }

    public void Explode()
    {
        isExploded = true;
        Debug.Log("<color=red>BOOM! Player Exploded!</color>");

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopLoopingSFX();
            AudioManager.Instance.PlaySFX("BombExplode");
        }

        // Play Explosion Particle Effect
        if (explosionParticlePrefab == null)
        {
#if UNITY_EDITOR
            explosionParticlePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<ParticleSystem>("Assets/_Project/Prefabs/ExplosionParticle.prefab");
#endif
        }

        Vector3 spawnPos = transform.position;
        if (explosionParticlePrefab != null)
        {
            ParticleSystem exp = Instantiate(explosionParticlePrefab, spawnPos, Quaternion.identity);
            exp.gameObject.SetActive(true);
            exp.Play(true);
            Destroy(exp.gameObject, 2.5f);
        }
        else
        {
            DustParticleEffects.PlayExplosion(spawnPos);
        }

        DustParticleEffects.PlayBloodSplatter(spawnPos);

        if (CameraFollow.Instance != null) CameraFollow.Instance.ShakeCamera(0.4f, 0.7f);
        gameObject.SetActive(false);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.TriggerGameOver();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawRay(transform.position, Vector2.down * groundCheckDistance);
    }
}
