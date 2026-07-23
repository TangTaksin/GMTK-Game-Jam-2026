using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveForce = 15f;
    [SerializeField] private float maxVelocity = 10f;
    [SerializeField] private float jumpForce = 8f;

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    [Header("Jump Settings")]
    [SerializeField] private int maxJumps = 2;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckDistance = 0.6f;

    private int jumpsRemaining;
    private bool isGrounded;

    private void Update()
    {
        CheckGrounded();

        if (Input.GetButtonDown("Jump") && jumpsRemaining > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f); // Reset Y velocity for consistent jump height
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            jumpsRemaining--;
        }
    }

    private void CheckGrounded()
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, groundCheckDistance, groundLayer);
        isGrounded = hit.collider != null;

        if (isGrounded && rb.linearVelocity.y <= 0.1f)
        {
            jumpsRemaining = maxJumps;
        }
    }

    [Header("Brake / Self-Stop System")]
    [SerializeField] private float brakeForce = 12f;
    [SerializeField] private float angularBrakeDamping = 10f;

    private void FixedUpdate()
    {
        float moveX = Input.GetAxisRaw("Horizontal");

        if (!Mathf.Approximately(moveX, 0f))
        {
            // Active player input
            rb.AddForce(new Vector2(moveX * moveForce, 0f), ForceMode2D.Force);

            if (Mathf.Abs(rb.linearVelocity.x) > maxVelocity)
            {
                rb.linearVelocity = new Vector2(Mathf.Sign(rb.linearVelocity.x) * maxVelocity, rb.linearVelocity.y);
            }
        }
        else
        {
            // Self-stop system: Apply active counter-braking force & angular damping when no input
            Vector2 currentVel = rb.linearVelocity;
            if (Mathf.Abs(currentVel.x) > 0.05f)
            {
                float targetVelX = Mathf.MoveTowards(currentVel.x, 0f, brakeForce * Time.fixedDeltaTime);
                rb.linearVelocity = new Vector2(targetVelX, currentVel.y);
            }

            // Slow down rotation spinning
            rb.angularVelocity = Mathf.MoveTowards(rb.angularVelocity, 0f, angularBrakeDamping * Time.fixedDeltaTime * 100f);
        }
    }
}
