using UnityEngine;

public class BlockMovement : MonoBehaviour
{
    private Vector2Int position;
    private GridObject gridObject;

    private void Start()
    {
        gridObject = GetComponent<GridObject>();
        position = gridObject.gridPosition;
        GridManager.Instance.RegisterBlock(position, gameObject);
    }

    public bool TryMove(Vector2Int direction)
    {
        Vector2Int newPosition = position + direction;

        if (!GridManager.Instance.IsWithinBounds(newPosition))
            return false;

        if (GridManager.Instance.IsCellOccupied(newPosition))
            return false;

        // Move the block
        GridManager.Instance.UnregisterBlock(position);
        position = newPosition;
        gridObject.gridPosition = position; // Update GridObject’s position
        GridManager.Instance.RegisterBlock(position, gameObject);

        return true;
    }
}
