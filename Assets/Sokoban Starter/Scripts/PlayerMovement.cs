using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float moveDelay = 0.2f;
    private Vector2Int position = new Vector2Int(1, 1);
    private float lastMoveTime;
    private GridObject gridObject;

    // Use GridManager's size if available
    private int gridWidth;
    private int gridHeight;

    private void Start()
    {
        gridObject = GetComponent<GridObject>();
        position = gridObject.gridPosition;

        // Get grid size from GridManager
        gridWidth = GridManager.Instance.gridWidth;
        gridHeight = GridManager.Instance.gridHeight;

        GridManager.Instance.RegisterBlock(position, gameObject);
    }

    private void Update()
{
    if (Time.time - lastMoveTime < moveDelay) return;

    Vector2Int moveDirection = Vector2Int.zero;

    if (Input.GetKeyDown(KeyCode.W)) moveDirection = Vector2Int.up;    // W moves up (positive y)
    else if (Input.GetKeyDown(KeyCode.S)) moveDirection = Vector2Int.down;  // S moves down (negative y)
    else if (Input.GetKeyDown(KeyCode.A)) moveDirection = Vector2Int.left;  // A moves left (negative x)
    else if (Input.GetKeyDown(KeyCode.D)) moveDirection = Vector2Int.right; // D moves right (positive x)

    if (moveDirection != Vector2Int.zero)
        TryMove(moveDirection);
}

    private void TryMove(Vector2Int direction)
    {
        Vector2Int newPosition = position + direction;

        if (newPosition.x < 1 || newPosition.x > gridWidth || newPosition.y < 1 || newPosition.y > gridHeight)
            return;

        if (position.x == 1 && direction.x < 0) return;
        if (position.y == 1 && direction.y < 0) return;

        if (GridManager.Instance.IsCellOccupied(newPosition))
            return;

        GridManager.Instance.UnregisterBlock(position);
        position = newPosition;
        gridObject.gridPosition = position;
        GridManager.Instance.RegisterBlock(position, gameObject);

        lastMoveTime = Time.time;
    }
}