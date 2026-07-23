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
    [SerializeField] private float removeBehindDistance = 15f;

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

    // Component references for 2D physics collision and rendering line
    private EdgeCollider2D edgeCollider;
    private LineRenderer lineRenderer;

    // Pre-allocated List for terrain points (reduces Garbage Collection overhead)
    private readonly List<Vector2> points = new List<Vector2>(128);
    
    // Array buffer passed to LineRenderer to avoid instantiating new Vector3[] every frame
    private Vector3[] linePositions = new Vector3[128];

    private float currentX;
    private float seed;
    private float cycleLength;
    private bool isDirty;

    private void Awake()
    {
        edgeCollider = GetComponent<EdgeCollider2D>();
        lineRenderer = GetComponent<LineRenderer>();
        
        // Randomize seed so each playthrough generates unique hills
        seed = Random.Range(0f, 1000f);
        
        // Calculate total cycle length (flat safe zone + hilly slope zone)
        cycleLength = flatZoneLength + slopeZoneLength;
    }

    private void Start()
    {
        // Auto-find player in scene if not assigned in Inspector
        if (player == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) player = p.transform;
        }

        // Generate initial terrain around player spawn
        GenerateTerrainAhead();
        UpdateComponents();
    }

    private void Update()
    {
        if (player == null) return;

        // Check if player is approaching end of generated terrain ahead
        bool needsGenerate = currentX - player.position.x < generateAheadDistance;
        
        // Check if old terrain points behind player exceed cutoff distance
        bool needsCleanup = points.Count > 0 && points[0].x < player.position.x - removeBehindDistance;

        if (needsGenerate)
        {
            GenerateTerrainAhead();
        }

        if (needsCleanup)
        {
            CleanupBehind();
        }

        // Update physics collider and visual rendering only when points change (Batch Update)
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
    /// Mathematical formula to calculate Y height at any given X position.
    /// </summary>
    private float CalculateHeightAt(float x)
    {
        // Get current position within cycle phase (0 to cycleLength)
        float positionInCycle = Mathf.Repeat(x, cycleLength);

        // 1. Safe Flat Zone: Return constant flat ground height for resetting player timer
        if (positionInCycle < flatZoneLength)
        {
            return (Mathf.PerlinNoise(seed + (x - positionInCycle) * frequency, 0f) * amplitude) + baseHeight;
        }

        // 2. Slope Zone: Combine Perlin Noise + Sine wave for dynamic hills
        float noise = Mathf.PerlinNoise(seed + x * frequency, 0f) * amplitude;
        float wave = Mathf.Sin(x * 0.2f) * 1.5f;
        return baseHeight + noise + wave;
    }

    /// <summary>
    /// Despawns old terrain points far behind the player (Memory Cleanup).
    /// </summary>
    private void CleanupBehind()
    {
        float cutoffX = player.position.x - removeBehindDistance;
        int removeCount = 0;

        // Count points exceeding cutoffX distance
        while (removeCount < points.Count && points[removeCount].x < cutoffX)
        {
            removeCount++;
        }

        // Batch remove points in a single call (RemoveRange) for performance
        if (removeCount > 0)
        {
            points.RemoveRange(0, removeCount);
            isDirty = true;
        }
    }

    /// <summary>
    /// Updates EdgeCollider2D physics points and LineRenderer visual positions.
    /// </summary>
    private void UpdateComponents()
    {
        int count = points.Count;
        if (count < 2) return;

        // 1. Update 2D physics collider points for Rigidbody2D interaction
        edgeCollider.SetPoints(points);

        // 2. Resize buffer array if point count exceeds current buffer size
        if (linePositions.Length < count)
        {
            linePositions = new Vector3[count * 2];
        }

        // 3. Convert Vector2 points into cached Vector3 buffer (Zero GC allocation)
        for (int i = 0; i < count; i++)
        {
            Vector2 p = points[i];
            linePositions[i] = new Vector3(p.x, p.y, 0f);
        }

        // 4. Render visual terrain line
        lineRenderer.positionCount = count;
        lineRenderer.SetPositions(linePositions);
    }
}
