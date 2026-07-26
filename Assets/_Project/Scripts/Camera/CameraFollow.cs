using UnityEngine;
using DG.Tweening;

public class CameraFollow : MonoBehaviour
{
    public static CameraFollow Instance { get; private set; }

    private enum CameraState
    {
        MenuFixed,     // Locked at Y = startCameraY (3.0)
        PanningDown,   // Smoothly animating Y from 3.0 down to targetPanY (0.0)
        GameplayFollow // Standard gameplay camera follow
    }

    [Header("Target Settings")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 baseOffset = new Vector3(0f, 1.5f, -10f);

    [Header("Start Menu Camera Pan")]
    [Tooltip("Target camera position X before pressing Start (Main Menu state).")]
    [SerializeField] private float startCameraX = 0.0f;

    [Tooltip("Target camera position Y before pressing Start (Main Menu state).")]
    [SerializeField] private float startCameraY = 3.0f;

    [Tooltip("Target camera position X to pan down to before player jump intro plays.")]
    [SerializeField] private float targetPanX = 0.0f;

    [Tooltip("Target camera position Y to pan down to before player jump intro plays.")]
    [SerializeField] private float targetPanY = 0.0f;

    [Tooltip("Duration in seconds to smoothly pan camera down when game starts.")]
    [SerializeField] private float panDownDuration = 1.2f;

    [Tooltip("Easing curve for panning camera down.")]
    [SerializeField] private Ease panEase = Ease.InOutCubic;

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

    [Header("Intro Camera Settings")]
    [Tooltip("If true, camera stays fixed at spawn position until player intro jump finishes.")]
    [SerializeField] private bool lockDuringIntro = false;

    private Rigidbody2D targetRb;
    private PlayerMovement targetMovement;
    private Camera cam;

    private float velocityX;
    private float velocityY;
    private float currentLookAheadX;
    private float currentVerticalOffset;

    private CameraState currentState = CameraState.MenuFixed;
    private float currentPanX = 0.0f;
    private float currentPanY = 3.0f;
    private Tween panTween;

    public static event System.Action OnCameraPanComplete;
    public bool IsPanningDown => currentState == CameraState.PanningDown;

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

    private void OnEnable()
    {
        GameManager.OnGameStart += HandleGameStart;
        GameManager.OnGameRestart += HandleGameRestart;
    }

    private void OnDisable()
    {
        GameManager.OnGameStart -= HandleGameStart;
        GameManager.OnGameRestart -= HandleGameRestart;
    }

    private void Start()
    {
        SetupInitialPosition();
    }

    private void SetupInitialPosition()
    {
        if (target != null)
        {
            // If game is not started yet (Start Menu active), lock initial position to (startCameraX=0, startCameraY=3)
            if (GameManager.Instance != null && !GameManager.Instance.IsGameStarted)
            {
                currentState = CameraState.MenuFixed;
                currentPanX = startCameraX;
                currentPanY = startCameraY;
                transform.position = new Vector3(startCameraX, startCameraY, baseOffset.z);
            }
            else
            {
                currentState = CameraState.GameplayFollow;
                Vector3 startPos = target.position + baseOffset;
                if (DynamicTerrainGenerator.Instance != null)
                {
                    startPos.y = DynamicTerrainGenerator.Instance.CalculateHeightAt(target.position.x) + baseOffset.y;
                }
                transform.position = startPos;
            }
        }
    }

    /// <summary>
    /// Starts panning the camera down from (0, 3.0) to (0, 0.0) if needed.
    /// Safely idempotent to support deterministic event triggering order.
    /// </summary>
    public void StartPanDownIfNeeded()
    {
        if (currentState == CameraState.PanningDown || currentState == CameraState.GameplayFollow)
        {
            return;
        }

        currentState = CameraState.PanningDown;
        currentPanX = startCameraX;
        currentPanY = startCameraY;

        panTween?.Kill();
        Sequence panSeq = DOTween.Sequence().SetLink(gameObject);
        panSeq.Join(DOTween.To(() => currentPanX, x => currentPanX = x, targetPanX, panDownDuration).SetEase(panEase));
        panSeq.Join(DOTween.To(() => currentPanY, y => currentPanY = y, targetPanY, panDownDuration).SetEase(panEase));
        panSeq.OnComplete(() =>
        {
            currentState = CameraState.GameplayFollow;
            OnCameraPanComplete?.Invoke();
        });
        panTween = panSeq;
    }

    private void HandleGameStart()
    {
        StartPanDownIfNeeded();
    }

    private void HandleGameRestart()
    {
        panTween?.Kill();
        currentState = CameraState.MenuFixed;
        currentPanX = startCameraX;
        currentPanY = startCameraY;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        panTween?.Kill();
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

        // State 1: Menu Fixed (Locked at X = startCameraX 0.0, Y = startCameraY 3.0)
        if (currentState == CameraState.MenuFixed)
        {
            transform.position = new Vector3(startCameraX, startCameraY, baseOffset.z);
            return;
        }

        // State 2: Panning Down (Smoothly animating X from startCameraX to targetPanX, Y from startCameraY to targetPanY 0.0)
        if (currentState == CameraState.PanningDown)
        {
            transform.position = new Vector3(currentPanX, currentPanY, baseOffset.z);
            return;
        }

        // State 3: Gameplay Follow
        if (lockDuringIntro && targetMovement != null && targetMovement.IsIntroJumping)
        {
            return;
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
