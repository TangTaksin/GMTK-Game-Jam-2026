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
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private string defuseAnimBoolName = "IsDefusing";
    [SerializeField] private float defuseSquashY = 0.75f;
    [SerializeField] private float defuseStretchX = 1.15f;
    [SerializeField] private float defuseWiggleSpeed = 25.0f;
    [SerializeField] private float defuseWiggleAngle = 4.0f;

    [Header("Ground Check")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckDistance = 0.8f;

    private Rigidbody2D rb;
    private PlayerMovement playerMovement;
    private bool isHeld;
    private bool isGrounded;
    private float groundRestTimer = 0f;
    private bool isExploded;

    private Vector3 initialPlayerScale = Vector3.one;
    private bool playerScaleCached = false;
    private bool wasDefusing = false;

    public float CurrentTime => currentTime;
    public int CurrentTimeDisplay => Mathf.CeilToInt(currentTime);
    public int MaxTime => maxTime;
    public bool IsHeld => isHeld;
    public bool IsGrounded => isGrounded;
    public float GroundRestTimer => groundRestTimer;
    public float ResetGroundDuration => resetGroundDuration;
    public bool IsDefusing => !isHeld && isGrounded && groundRestTimer < resetGroundDuration;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        currentTime = maxTime;
        isHeld = startHeld;

        FindPlayer();
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
            if (!playerScaleCached)
            {
                initialPlayerScale = playerTransform.localScale;
                playerScaleCached = true;
            }
            return;
        }

        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            playerMovement = player.GetComponent<PlayerMovement>();
            if (!playerScaleCached)
            {
                initialPlayerScale = playerTransform.localScale;
                playerScaleCached = true;
            }
        }
    }

    private void Update()
    {
        if (isExploded) return;
        if (playerTransform == null) FindPlayer();

        // Check if player has movement input or is moving
        float moveX = Input.GetAxisRaw("Horizontal");
        if (moveX < 0f) moveX = 0f; // Ignore left input
        bool hasInput = moveX > 0.05f || Input.GetButton("Jump");

        Rigidbody2D playerRb = (playerTransform != null) ? playerTransform.GetComponent<Rigidbody2D>() : null;
        bool isPlayerMoving = playerRb != null && playerRb.linearVelocity.magnitude > 0.15f;
        bool isIntroJumping = playerMovement != null && playerMovement.IsIntroJumping;

        bool shouldHoldBomb = hasInput || isPlayerMoving || isIntroJumping;

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
            if (!isIntroJumping)
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
                // Count up rest timer to 3 seconds on ground to reset bomb time
                groundRestTimer += Time.deltaTime;

                if (groundRestTimer >= resetGroundDuration)
                {
                    currentTime = maxTime;
                }
            }
            else
            {
                groundRestTimer = 0f;
            }
        }

        UpdateDefuseAnimation();

        if (currentTime <= 0f)
        {
            Explode();
        }
    }

    private void UpdateDefuseAnimation()
    {
        if (playerTransform == null) return;

        if (!playerScaleCached)
        {
            initialPlayerScale = playerTransform.localScale;
            playerScaleCached = true;
        }

        bool isDefusingNow = IsDefusing;

        if (playerAnimator == null && playerTransform != null)
        {
            playerAnimator = playerTransform.GetComponent<Animator>();
            if (playerAnimator == null) playerAnimator = playerTransform.GetComponentInChildren<Animator>();
        }

        if (playerAnimator != null)
        {
            playerAnimator.SetBool(defuseAnimBoolName, isDefusingNow);
        }

        if (enableProceduralDefuseAnim)
        {
            if (isDefusingNow)
            {
                wasDefusing = true;
                // Crouching defuse scale facing the bomb
                Vector3 defuseScale = new Vector3(
                    initialPlayerScale.x * defuseStretchX,
                    initialPlayerScale.y * defuseSquashY,
                    initialPlayerScale.z
                );

                // Intense fast-hands defuse wiggle animation
                float wiggleZ = Mathf.Sin(Time.time * defuseWiggleSpeed) * defuseWiggleAngle;

                playerTransform.localScale = defuseScale;
                playerTransform.localRotation = Quaternion.Euler(0f, 0f, wiggleZ);
            }
            else if (wasDefusing)
            {
                wasDefusing = false;
                // Reset player scale and rotation back to normal
                playerTransform.localScale = initialPlayerScale;
                playerTransform.localRotation = Quaternion.identity;
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
