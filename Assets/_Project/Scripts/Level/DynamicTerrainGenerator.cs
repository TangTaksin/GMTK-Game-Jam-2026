using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Dynamic continuous 2D procedural terrain generator.
/// Generates terrain height real-time using Perlin Noise + Sine Wave formulas without requiring prefabs.
/// </summary>
[RequireComponent(typeof(EdgeCollider2D))]
[RequireComponent(typeof(LineRenderer))]
public class DynamicTerrainGenerator : MonoBehaviour
{
    [Header("Tracking")]
    [Tooltip("Target player transform to track for level generation.")]
    [SerializeField] private Transform player;
    
    [Tooltip("Distance ahead of the player to generate new terrain.")]
    [SerializeField] private float generateAheadDistance = 30f;
    
    [Tooltip("Distance behind the player to remove old terrain for memory optimization.")]
    [SerializeField] private float removeBehindDistance = 35f;

    [Header("Terrain Resolution")]
    [Tooltip("Horizontal step interval between terrain points (smaller = higher detail).")]
    [SerializeField] private float xStep = 0.5f;

    [Header("Noise / Wave Parameters")]
    [Tooltip("Frequency of terrain hills (lower = long hills, higher = short frequent hills).")]
    [SerializeField] private float frequency = 0.05f;
    
    [Tooltip("Max height/amplitude of terrain hills.")]
    [SerializeField] private float amplitude = 4.0f;
    
    [Tooltip("Base ground Y position.")]
    [SerializeField] private float baseHeight = -3.0f;

    [Header("Pacing & Safe Flat Zones")]
    [Tooltip("Length of guaranteed flat ground zones for resetting the player timer.")]
    [SerializeField] private float flatZoneLength = 8.0f;
    
    [Tooltip("Length of hilly slope zones where player slides and timer ticks down.")]
    [SerializeField] private float slopeZoneLength = 16.0f;

    [Tooltip("Transition length in units to smoothly blend between flat zones and slope zones.")]
    [SerializeField] private float transitionLength = 2.0f;

    [Header("Start Boundary / Left Wall")]
    [Tooltip("Distance behind player spawn to start ground generation.")]
    [SerializeField] private float startBehindOffset = 10f;

    [Tooltip("Automatically create an invisible wall to prevent player from falling left off the map.")]
    [SerializeField] private bool createLeftWall = true;

    [Tooltip("X position of the left invisible boundary wall.")]
    [SerializeField] private float leftWallX = -5f;

    [Tooltip("Height of the left invisible boundary wall.")]
    [SerializeField] private float leftWallHeight = 30f;

    // Component references
    private EdgeCollider2D edgeCollider;
    private LineRenderer lineRenderer;
    private Transform leftWallTransform;

    // Pre-allocated collections to minimize GC allocations
    private readonly List<Vector2> points = new List<Vector2>(128);
    private readonly List<Vector2> localPoints = new List<Vector2>(128);
    private Vector3[] linePositions = new Vector3[128];

    private float currentX;
    private float seed;
    private bool isDirty;

    private float CycleLength => flatZoneLength + slopeZoneLength;

    private void Awake()
    {
        // Enforce world origin anchor so Local Space == World Space for EdgeCollider2D
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.one;

        edgeCollider = GetComponent<EdgeCollider2D>();
        lineRenderer = GetComponent<LineRenderer>();
        
        seed = Random.Range(0f, 1000f);
    }

    private void Start()
    {
        // Auto-find player in scene if not assigned in Inspector
        if (player == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) player = p.transform;
        }

        if (createLeftWall)
        {
            CreateLeftBoundaryWall();
        }

        if (player == null)
        {
            Debug.LogWarning("[DynamicTerrainGenerator] No player transform assigned or tagged 'Player' found in scene.", this);
            return;
        }

        // Start generating terrain behind player spawn position to prevent falling
        currentX = player.position.x - startBehindOffset;

        // Generate initial terrain around player spawn
        GenerateTerrainAhead();
        UpdateComponents();
    }

    private void CreateLeftBoundaryWall()
    {
        GameObject wallObj = new GameObject("LeftBoundaryWall");
        wallObj.transform.SetParent(transform, false);
        leftWallTransform = wallObj.transform;
        leftWallTransform.position = new Vector3(leftWallX, baseHeight + leftWallHeight / 2f, 0f);

        BoxCollider2D wallCollider = wallObj.AddComponent<BoxCollider2D>();
        wallCollider.size = new Vector2(2f, leftWallHeight);
    }

    private void Update()
    {
        if (player == null) return;

        float playerX = player.position.x;

        // Check if player is approaching end of generated terrain ahead
        if (currentX - playerX < generateAheadDistance)
        {
            GenerateTerrainAhead();
        }

        // Check if old terrain points behind player exceed cutoff distance
        if (points.Count > 0 && points[0].x < playerX - removeBehindDistance)
        {
            CleanupBehind();
        }

        // Update physics collider and visual rendering only when points change
        if (isDirty)
        {
            UpdateComponents();
            isDirty = false;
        }
    }

    /// <summary>
    /// Generates terrain points ahead until currentX reaches target generateAheadDistance.
    /// </summary>
    private void GenerateTerrainAhead()
    {
        if (player == null) return;

        float targetX = player.position.x + generateAheadDistance;

        while (currentX < targetX)
        {
            float y = CalculateHeightAt(currentX);
            points.Add(new Vector2(currentX, y));
            currentX += xStep;
            isDirty = true;
        }
    }

    /// <summary>
    /// Mathematical formula to calculate Y height at any given X position with smooth transition blending.
    /// </summary>
    private float CalculateHeightAt(float x)
    {
        float cycle = CycleLength;
        if (cycle <= 0f) return baseHeight;

        // Get current position within cycle phase (0 to cycle)
        float positionInCycle = Mathf.Repeat(x, cycle);

        // Calculate constant flat ground height for current cycle
        float flatX = x - positionInCycle;
        float flatHeight = (Mathf.PerlinNoise(seed + flatX * frequency, 0f) * amplitude) + baseHeight;

        // Calculate dynamic slope height at position x
        float noise = Mathf.PerlinNoise(seed + x * frequency, 0f) * amplitude;
        float wave = Mathf.Sin(x * 0.2f) * 1.5f;
        float slopeHeight = baseHeight + noise + wave;

        // Clamp transition length to half of flatZoneLength to prevent overlap
        float maxTransition = flatZoneLength * 0.4f;
        float actualTransition = Mathf.Min(transitionLength, maxTransition);

        if (actualTransition <= 0.0001f)
        {
            return positionInCycle < flatZoneLength ? flatHeight : slopeHeight;
        }

        // 1. Transition from previous Slope Zone into Flat Zone
        if (positionInCycle < actualTransition)
        {
            float t = positionInCycle / actualTransition;
            return Mathf.Lerp(flatHeight, slopeHeight, Mathf.SmoothStep(1f, 0f, t));
        }

        // 2. Pure Safe Flat Zone
        if (positionInCycle < flatZoneLength - actualTransition)
        {
            return flatHeight;
        }

        // 3. Transition from Flat Zone into Slope Zone
        if (positionInCycle < flatZoneLength)
        {
            float t = (positionInCycle - (flatZoneLength - actualTransition)) / actualTransition;
            return Mathf.Lerp(flatHeight, slopeHeight, Mathf.SmoothStep(0f, 1f, t));
        }

        // 4. Pure Slope Zone
        return slopeHeight;
    }

    /// <summary>
    /// Despawns old terrain points far behind the player (Memory Cleanup).
    /// </summary>
    private void CleanupBehind()
    {
        if (player == null) return;

        float cutoffX = player.position.x - removeBehindDistance;
        int removeCount = 0;

        // Count points exceeding cutoffX distance
        while (removeCount < points.Count && points[removeCount].x < cutoffX)
        {
            removeCount++;
        }

        // Batch remove points in a single call for performance
        if (removeCount > 0)
        {
            points.RemoveRange(0, removeCount);
            isDirty = true;
        }
    }

    /// <summary>
    /// Updates EdgeCollider2D physics points and LineRenderer visual positions.
    /// Converts World Space terrain calculations into EdgeCollider2D Local Space coordinates.
    /// </summary>
    private void UpdateComponents()
    {
        int count = points.Count;
        if (count < 2) return;

        if (linePositions.Length < count)
        {
            linePositions = new Vector3[count * 2];
        }

        bool isAtOrigin = transform.position == Vector3.zero && transform.rotation == Quaternion.identity && transform.localScale == Vector3.one;

        if (isAtOrigin)
        {
            // Zero offset optimization: direct pass-through
            edgeCollider.SetPoints(points);

            for (int i = 0; i < count; i++)
            {
                Vector2 p = points[i];
                linePositions[i] = new Vector3(p.x, p.y, 0f);
            }
        }
        else
        {
            // Convert World Space points into Local Space for EdgeCollider2D
            localPoints.Clear();
            for (int i = 0; i < count; i++)
            {
                Vector2 worldP = points[i];
                Vector3 localP = transform.InverseTransformPoint(new Vector3(worldP.x, worldP.y, 0f));
                localPoints.Add(new Vector2(localP.x, localP.y));
                linePositions[i] = localP;
            }

            edgeCollider.SetPoints(localPoints);
        }

        // Render visual terrain line
        lineRenderer.useWorldSpace = isAtOrigin;
        lineRenderer.positionCount = count;
        lineRenderer.SetPositions(linePositions);

        // Dynamic safety wall trailing at the leftmost edge of existing terrain
        if (createLeftWall && leftWallTransform != null)
        {
            float safeLeftX = points[0].x + 0.5f;
            leftWallTransform.position = new Vector3(safeLeftX, baseHeight + leftWallHeight / 2f, leftWallTransform.position.z);
        }
    }
}

