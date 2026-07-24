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

    private Rigidbody2D rb;
    private BoxCollider2D boxCollider;
    private bool isThreatActive;
    private float graceTimer;
    private Rigidbody2D playerRb;

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
    }

    private void FixedUpdate()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;
        if (playerTransform == null) return;

        // Check if threat has been activated by player movement
        if (!isThreatActive)
        {
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
            float groundY = DynamicTerrainGenerator.Instance.CalculateHeightAt(nextPos.x);
            float halfHeight = boxCollider != null ? boxCollider.size.y * transform.localScale.y / 2f : 15f;
            nextPos.y = groundY + halfHeight + yOffsetFromGround;
        }

        if (rb != null && rb.bodyType != RigidbodyType2D.Kinematic)
        {
            rb.MovePosition(nextPos);
        }
        else
        {
            transform.position = nextPos;
        }
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
}

