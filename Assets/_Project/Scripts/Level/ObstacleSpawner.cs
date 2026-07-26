using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Cookie Run style procedural ground obstacle spawner.
/// Places ground spikes ahead of the player along terrain curves for jump challenges.
/// </summary>
public class ObstacleSpawner : MonoBehaviour
{
    [Header("Tracking & Distances")]
    [SerializeField] private Transform player;
    [SerializeField] private float generateAheadDistance = 35f;
    [SerializeField] private float removeBehindDistance = 30f;
    [SerializeField] private float initialSafeZoneEnd = 20f;

    [Header("Spawn Frequency")]
    [Tooltip("Minimum distance interval between spawned obstacles.")]
    [SerializeField] private float minInterval = 12f;
    [Tooltip("Maximum distance interval between spawned obstacles.")]
    [SerializeField] private float maxInterval = 20f;

    [Header("Height Offsets & Depth")]
    [Tooltip("Y offset above terrain surface for Ground Spikes.")]
    [SerializeField] private float groundYOffset = 0.5f;
    [Tooltip("Z position depth offset (0.15f places it behind the ground fill mesh).")]
    [SerializeField] private float zOffset = 0.15f;

    [Header("Sorting Layer Settings")]
    [Tooltip("Sorting layer name for obstacle sprite (matches groundSortingLayer).")]
    [SerializeField] private string obstacleSortingLayer = "Default";
    [Tooltip("Sorting order for obstacle sprite (set lower than ground, e.g. -1, to render behind ground fill).")]
    [SerializeField] private int obstacleSortingOrder = -1;

    [Header("Prefab (Optional - Procedural fallback used if null)")]
    [SerializeField] private GameObject groundObstaclePrefab;

    private readonly List<GameObject> activeObstacles = new List<GameObject>();
    private float nextSpawnX;

    public static ObstacleSpawner Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) player = p.transform;
        }

        if (player != null)
        {
            nextSpawnX = Mathf.Max(player.position.x + initialSafeZoneEnd, initialSafeZoneEnd);
        }
        else
        {
            nextSpawnX = initialSafeZoneEnd;
        }

        SpawnAhead();
    }

    private void Update()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) player = p.transform;
            return;
        }

        float playerX = player.position.x;

        if (nextSpawnX - playerX < generateAheadDistance)
        {
            SpawnAhead();
        }

        CleanupBehind(playerX);
    }

    private void SpawnAhead()
    {
        if (DynamicTerrainGenerator.Instance == null) return;

        float targetX = (player != null ? player.position.x : 0f) + generateAheadDistance;

        while (nextSpawnX < targetX)
        {
            SpawnSingleObstacleAt(nextSpawnX);
            float randomGap = Random.Range(minInterval, maxInterval);
            nextSpawnX += randomGap;
        }
    }

    private void SpawnSingleObstacleAt(float spawnX)
    {
        if (DynamicTerrainGenerator.Instance == null) return;

        ObstacleType type = ObstacleType.GroundSpike;
        float groundY = DynamicTerrainGenerator.Instance.CalculateHeightAt(spawnX);
        Vector3 spawnPos = new Vector3(spawnX, groundY + groundYOffset, zOffset);

        GameObject obstacleObj;

        if (groundObstaclePrefab != null)
        {
            obstacleObj = Instantiate(groundObstaclePrefab, spawnPos, Quaternion.identity, transform);
        }
        else
        {
            // Procedural Ground Spike Creation
            obstacleObj = new GameObject("GroundSpike_Obstacle");
            obstacleObj.transform.SetParent(transform);
            obstacleObj.transform.position = spawnPos;

            BoxCollider2D boxCol = obstacleObj.AddComponent<BoxCollider2D>();
            boxCol.isTrigger = true;
            boxCol.size = new Vector2(0.8f, 1.2f);

            Obstacle obstacleComponent = obstacleObj.AddComponent<Obstacle>();
            obstacleComponent.Initialize(type);
        }

        // Set sorting layer & order to render behind ground fill
        Obstacle obsComp = obstacleObj.GetComponent<Obstacle>();
        if (obsComp != null)
        {
            obsComp.SetSorting(obstacleSortingLayer, obstacleSortingOrder);
        }
        else
        {
            SpriteRenderer sr = obstacleObj.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                sr.sortingLayerName = obstacleSortingLayer;
                sr.sortingOrder = obstacleSortingOrder;
            }
        }

        // Align obstacle rotation with terrain slope
        float delta = 0.2f;
        float yLeft = DynamicTerrainGenerator.Instance.CalculateHeightAt(spawnX - delta);
        float yRight = DynamicTerrainGenerator.Instance.CalculateHeightAt(spawnX + delta);
        Vector2 slopeTangent = new Vector2(delta * 2f, yRight - yLeft).normalized;
        float slopeAngle = Mathf.Atan2(slopeTangent.y, slopeTangent.x) * Mathf.Rad2Deg;

        obstacleObj.transform.rotation = Quaternion.Euler(0f, 0f, slopeAngle);

        activeObstacles.Add(obstacleObj);
    }

    private void CleanupBehind(float playerX)
    {
        float cutoffX = playerX - removeBehindDistance;

        for (int i = activeObstacles.Count - 1; i >= 0; i--)
        {
            GameObject obs = activeObstacles[i];
            if (obs == null)
            {
                activeObstacles.RemoveAt(i);
            }
            else if (obs.transform.position.x < cutoffX)
            {
                activeObstacles.RemoveAt(i);
                Destroy(obs);
            }
        }
    }
}
