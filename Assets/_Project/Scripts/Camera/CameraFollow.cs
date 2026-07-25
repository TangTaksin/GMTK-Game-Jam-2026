using UnityEngine;
using DG.Tweening;

public class CameraFollow : MonoBehaviour
{
    public static CameraFollow Instance { get; private set; }

    [Header("Target Settings")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 baseOffset = new Vector3(0f, 1.5f, -10f);

    [Header("Smooth Damping")]
    [SerializeField] private float smoothTimeX = 0.15f;
    [SerializeField] private float smoothTimeY = 0.25f;
    [SerializeField] private float jumpUpSmoothTimeY = 0.45f; // Slower Y follow when jumping up so ground stays visible

    [Header("Forward Look-Ahead")]
    [SerializeField] private float lookAheadDistance = 3.5f; // Shift camera ahead in moving direction
    [SerializeField] private float lookAheadSmoothSpeed = 3f;

    [Header("Airborne Ground Visibility (Look Down)")]
    [SerializeField] private float airborneLookDownDistance = 3.5f; // Shift camera down when falling to show landing ground
    [SerializeField] private float airborneUpwardOffset = 2.0f; // Offset camera down relative to player when jumping high
    [SerializeField] private float verticalOffsetSmoothSpeed = 3f;

    [Header("Dynamic Speed & Air Zoom")]
    [SerializeField] private bool enableSpeedZoom = true;
    [SerializeField] private float minOrthoSize = 7f;
    [SerializeField] private float maxOrthoSize = 11f;
    [SerializeField] private float minZoomSpeed = 8f;
    [SerializeField] private float maxZoomSpeed = 22f;
    [SerializeField] private float zoomSmoothSpeed = 2f;

    private Rigidbody2D targetRb;
    private PlayerMovement targetMovement;
    private Camera cam;

    private float velocityX;
    private float velocityY;
    private float currentLookAheadX;
    private float currentVerticalOffset;

    private void Awake()
    {
        if (Instance == null) Instance = this;

        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;

        if (target != null)
        {
            InitTargetComponents();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        transform.DOKill();
    }

    private void InitTargetComponents()
    {
        if (target != null)
        {
            targetRb = target.GetComponent<Rigidbody2D>();
            targetMovement = target.GetComponent<PlayerMovement>();
        }
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        InitTargetComponents();
    }

    public void ShakeCamera(float duration = 0.35f, float strength = 0.6f)
    {
        transform.DOKill();
        transform.DOShakePosition(duration, strength, 14, 90f, false, true).SetUpdate(true);
    }

    private void LateUpdate()
    {
        if (target == null) return;

        if (targetRb == null || targetMovement == null)
        {
            InitTargetComponents();
        }

        Vector3 targetPos = target.position + baseOffset;

        // 1. Horizontal Look-Ahead (Shift camera in front of player when moving)
        float targetLookAheadX = 0f;
        if (targetRb != null)
        {
            float velX = targetRb.linearVelocity.x;
            if (Mathf.Abs(velX) > 1f)
            {
                targetLookAheadX = Mathf.Sign(velX) * lookAheadDistance;
            }
        }
        currentLookAheadX = Mathf.Lerp(currentLookAheadX, targetLookAheadX, lookAheadSmoothSpeed * Time.deltaTime);
        targetPos.x += currentLookAheadX;

        // 2. Vertical Ground Bias / Airborne Look-Down (Keep ground in view when airborne)
        float targetVertOffset = 0f;
        bool isAirborne = targetMovement != null && !targetMovement.IsGrounded;

        if (isAirborne)
        {
            float velY = (targetRb != null) ? targetRb.linearVelocity.y : 0f;
            if (velY < -0.5f)
            {
                // Falling: Shift camera downward significantly to see landing spot
                targetVertOffset = -airborneLookDownDistance;
            }
            else
            {
                // Jumping upward: Keep camera lower relative to player to maintain ground view
                targetVertOffset = -airborneUpwardOffset;
            }
        }

        currentVerticalOffset = Mathf.Lerp(currentVerticalOffset, targetVertOffset, verticalOffsetSmoothSpeed * Time.deltaTime);
        targetPos.y += currentVerticalOffset;

        // 3. SmoothDamp Position (Slower Y damp when launching up)
        float velYCurrent = (targetRb != null) ? targetRb.linearVelocity.y : 0f;
        float currentSmoothTimeY = (isAirborne && velYCurrent > 0.5f) ? jumpUpSmoothTimeY : smoothTimeY;

        Vector3 currentPos = transform.position;
        float newX = Mathf.SmoothDamp(currentPos.x, targetPos.x, ref velocityX, smoothTimeX);
        float newY = Mathf.SmoothDamp(currentPos.y, targetPos.y, ref velocityY, currentSmoothTimeY);

        transform.position = new Vector3(newX, newY, targetPos.z);

        // 4. Dynamic Camera Zoom (Speed & Airborne Zoom Out)
        if (cam != null && cam.orthographic && enableSpeedZoom)
        {
            float currentSpeed = (targetRb != null) ? targetRb.linearVelocity.magnitude : 0f;
            float speedRatio = Mathf.Clamp01((currentSpeed - minZoomSpeed) / (maxZoomSpeed - minZoomSpeed));
            float targetOrthoSize = Mathf.Lerp(minOrthoSize, maxOrthoSize, speedRatio);

            // Zoom out slightly when airborne to show wider view of ground and obstacles
            if (isAirborne)
            {
                targetOrthoSize = Mathf.Max(targetOrthoSize, minOrthoSize + 1.5f);
            }

            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetOrthoSize, zoomSmoothSpeed * Time.deltaTime);
        }
    }
}
