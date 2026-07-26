using UnityEngine;

/// <summary>
/// Creates an invisible wall (BoxCollider2D) at the start of the map to prevent the player from falling off to the left.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class StartBoundary : MonoBehaviour
{
    [Header("Wall Settings")]
    [SerializeField] private float wallXPosition = -5f;
    [SerializeField] private float wallYPosition = 5f;
    [SerializeField] private float wallWidth = 2f;
    [SerializeField] private float wallHeight = 30f;
    [SerializeField] private LayerMask groundLayer;

    private BoxCollider2D boxCollider;

    private void Awake()
    {
        boxCollider = GetComponent<BoxCollider2D>();
        SetupWall();
    }

    private void SetupWall()
    {
        transform.position = new Vector3(wallXPosition, wallYPosition, 0f);
        boxCollider.size = new Vector2(wallWidth, wallHeight);
        boxCollider.isTrigger = false;

        // Set layer if assigned
        if (groundLayer != 0)
        {
            // Keep object on appropriate layer if needed
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.5f); // Red transparent box for Editor visualization
        Vector3 pos = transform.position;
        if (!Application.isPlaying && boxCollider != null)
        {
            pos = new Vector3(wallXPosition, wallYPosition, 0f);
            Gizmos.DrawCube(pos, new Vector3(wallWidth, wallHeight, 1f));
        }
        else
        {
            Gizmos.DrawCube(transform.position, new Vector3(boxCollider.size.x, boxCollider.size.y, 1f));
        }
    }
}
