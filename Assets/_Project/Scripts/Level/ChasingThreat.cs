using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ChasingThreat : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private float baseSpeed = 3.5f;
    [SerializeField] private float maxDistanceBehind = 14f;
    [SerializeField] private float speedIncreasePerMeter = 0.01f;

    [Header("Collision Settings")]
    [SerializeField] private string playerTag = "Player";

    [Header("Activation Settings")]
    [SerializeField] private float startGraceDelay = 1.0f; // Seconds after first player input before threat moves

    [Header("Terrain Curve Settings")]
    [SerializeField] private bool followTerrainCurve = true;
    [SerializeField] private float yOffsetFromGround = 0f;

    [Header("Auto Slope & Rotation Settings")]
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

    private Rigidbody2D rb;
    private BoxCollider2D boxCollider;
    private SpriteRenderer spriteRenderer;
    private bool isThreatActive;
    private float graceTimer;
    private Rigidbody2D playerRb;
    private Vector3 initialScale = Vector3.one;

    public void Initialize(float speed, float speedIncrease, float maxDistance, float graceDelay)
    {
        baseSpeed = speed;
        speedIncreasePerMeter = speedIncrease;
        maxDistanceBehind = maxDistance;
        startGraceDelay = graceDelay;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        initialScale = transform.localScale;
    }

    private void Start()
    {
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindWithTag(playerTag);
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
            }
        }

        if (playerTransform != null)
        {
            playerRb = playerTransform.GetComponent<Rigidbody2D>();
        }

        // Snap position and slope rotation immediately on start
        SnapToGround();
    }

    private void Update()
    {
        if (!enableSquashAndStretch || !isThreatActive) return;

        // Normal movement pulse (Squash & Stretch)
        float pulse = Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
        transform.localScale = new Vector3(
            initialScale.x * (1f + pulse),
            initialScale.y * (1f - pulse * 0.5f),
            initialScale.z
        );
    }

    private void SnapToGround()
    {
        if (DynamicTerrainGenerator.Instance == null) return;

        if (followTerrainCurve)
        {
            float targetY = CalculateTargetY(transform.position.x);
            Vector3 pos = transform.position;
            pos.y = targetY;
            transform.position = pos;
        }

        UpdateSlopeRotation(transform.position.x);
    }

    private float CalculateTargetY(float currentX)
    {
        if (DynamicTerrainGenerator.Instance == null) return transform.position.y;

        float sampleX = currentX;
        float bottomOffset = 0f;

        if (boxCollider != null)
        {
            sampleX += boxCollider.offset.x * transform.localScale.x;
            float localBottom = boxCollider.offset.y - (boxCollider.size.y / 2f);
            bottomOffset = localBottom * transform.localScale.y;
        }
        else if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            float localBottom = -spriteRenderer.bounds.extents.y;
            bottomOffset = localBottom;
        }
        else
        {
            bottomOffset = -0.5f * transform.localScale.y;
        }

        float groundY = DynamicTerrainGenerator.Instance.CalculateHeightAt(sampleX);
        return groundY - bottomOffset + yOffsetFromGround;
    }

    private void UpdateSlopeRotation(float currentX)
    {
        if (!autoSlopeRotation || DynamicTerrainGenerator.Instance == null) return;

        float delta = Mathf.Max(0.1f, slopeSampleDistance);
        float yLeft = DynamicTerrainGenerator.Instance.CalculateHeightAt(currentX - delta);
        float yRight = DynamicTerrainGenerator.Instance.CalculateHeightAt(currentX + delta);

        Vector2 slopeTangent = new Vector2(delta * 2f, yRight - yLeft).normalized;
        float targetAngle = Mathf.Atan2(slopeTangent.y, slopeTangent.x) * Mathf.Rad2Deg;

        targetAngle = Mathf.Clamp(targetAngle, -maxTiltAngle, maxTiltAngle);

        if (rb != null && rb.bodyType != RigidbodyType2D.Kinematic)
        {
            float currentAngle = rb.rotation;
            float smoothAngle = Mathf.LerpAngle(currentAngle, targetAngle, Time.fixedDeltaTime * rotationSpeed);
            rb.MoveRotation(smoothAngle);
        }
        else
        {
            float currentAngle = transform.eulerAngles.z;
            float smoothAngle = Mathf.LerpAngle(currentAngle, targetAngle, Time.fixedDeltaTime * rotationSpeed);
            transform.rotation = Quaternion.Euler(0f, 0f, smoothAngle);
        }
    }

    private void FixedUpdate()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;

        // Keep threat snapped to ground height and slope rotation even before activation
        if (!isThreatActive)
        {
            SnapToGround();

            if (playerTransform == null) return;

            bool hasPlayerInput = Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.01f || Input.GetButton("Jump");
            bool isPlayerMoving = playerRb != null && playerRb.linearVelocity.magnitude > 0.1f;

            if (hasPlayerInput || isPlayerMoving)
            {
                graceTimer += Time.fixedDeltaTime;
                if (graceTimer >= startGraceDelay)
                {
                    isThreatActive = true;
                }
            }
            return;
        }

        if (playerTransform == null) return;

        // Calculate difficulty scaling speed based on player X position
        float playerX = playerTransform.position.x;
        float extraSpeed = Mathf.Max(0f, playerX * speedIncreasePerMeter);
        float targetSpeed = baseSpeed + extraSpeed;

        // Catch-up logic if player runs too far ahead
        float distanceBehind = playerX - transform.position.x;
        if (distanceBehind > maxDistanceBehind)
        {
            // Teleport or catch up smoothly so threat stays on screen edge
            float catchUpSpeed = targetSpeed + (distanceBehind - maxDistanceBehind) * 2f;
            targetSpeed = catchUpSpeed;
        }

        Vector2 nextPos = new Vector2(transform.position.x + targetSpeed * Time.fixedDeltaTime, transform.position.y);

        if (followTerrainCurve && DynamicTerrainGenerator.Instance != null)
        {
            nextPos.y = CalculateTargetY(nextPos.x);
        }

        if (rb != null && rb.bodyType != RigidbodyType2D.Kinematic)
        {
            rb.MovePosition(nextPos);
        }
        else
        {
            transform.position = nextPos;
        }

        // Apply smooth slope rotation matching terrain curves
        UpdateSlopeRotation(nextPos.x);
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
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;

        if (target.CompareTag(playerTag) || target.GetComponent<PlayerMovement>() != null)
        {
            Debug.Log("<color=red>[ChasingThreat] Player caught by threat!</color>");
            
            PlayerTimer playerTimer = target.GetComponent<PlayerTimer>();
            if (playerTimer != null)
            {
                playerTimer.Explode();
            }
            else
            {
                GameManager.Instance.TriggerGameOver();
            }
        }
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
    }
}

