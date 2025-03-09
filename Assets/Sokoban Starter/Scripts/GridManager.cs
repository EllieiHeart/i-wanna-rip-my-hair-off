using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    private Dictionary<Vector2Int, GridObject> gridObjects = new Dictionary<Vector2Int, GridObject>();
    private GridObject player;

    private int gridWidth = 10;
    private int gridHeight = 5;
    private Vector2Int gridOffset = new Vector2Int(1, 1); // Adjusting for player's start position

    private void Start()
    {
        InitializeGrid();
    }

    private void Update()
    {
        HandlePlayerInput();
    }

    private void InitializeGrid()
    {
        GridObject[] objects = FindObjectsByType<GridObject>(FindObjectsSortMode.None);
        foreach (var obj in objects)
        {
            obj.gridPosition += gridOffset; // Adjust initial positions
            if (!gridObjects.ContainsKey(obj.gridPosition))
            {
                gridObjects[obj.gridPosition] = obj;
            }

            if (obj.CompareTag("Player"))
            {
                player = obj;
            }
        }
    }

    private void HandlePlayerInput()
    {
        Vector2Int direction = Vector2Int.zero;
        if (Input.GetKeyDown(KeyCode.W)) direction = Vector2Int.down;
        if (Input.GetKeyDown(KeyCode.S)) direction = Vector2Int.up;
        if (Input.GetKeyDown(KeyCode.A)) direction = Vector2Int.left;
        if (Input.GetKeyDown(KeyCode.D)) direction = Vector2Int.right;

        if (direction != Vector2Int.zero)
        {
            MoveBlock(player, direction);
        }
    }

    private bool MoveBlock(GridObject block, Vector2Int direction)
    {
        Vector2Int newPosition = block.gridPosition + direction;

        // Ensure movement stays within grid bounds (1 ≤ x < 11, 1 ≤ y < 6)
        if (newPosition.x < gridOffset.x || newPosition.x >= gridWidth + gridOffset.x ||
            newPosition.y < gridOffset.y || newPosition.y >= gridHeight + gridOffset.y)
            return false;

        // Check if the new position is already occupied by another block
        if (gridObjects.TryGetValue(newPosition, out GridObject other))
        {
            if (other.CompareTag("Wall")) return false; // Walls cannot be moved or passed
            if (other.CompareTag("Smooth"))
            {
                if (!MoveBlock(other, direction)) return false; // Try to push the smooth block
            }
            if (other.CompareTag("Sticky"))
            {
                if (!MoveSticky(other, direction)) return false; // Try to move sticky with it
            }
            if (other.CompareTag("Clingy"))
            {
                return false; // Clingy cannot be pushed
            }
        }

        // Update the block's position in the dictionary
        gridObjects.Remove(block.gridPosition);
        block.gridPosition = newPosition;
        gridObjects[newPosition] = block;

        return true;
    }

    private bool MoveSticky(GridObject sticky, Vector2Int direction)
    {
        List<GridObject> toMove = new List<GridObject> { sticky };

        // Find all adjacent sticky blocks that need to move
        foreach (Vector2Int adj in new Vector2Int[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
        {
            Vector2Int adjPos = sticky.gridPosition + adj;
            if (gridObjects.TryGetValue(adjPos, out GridObject adjacentBlock) && adjacentBlock.CompareTag("Sticky"))
            {
                toMove.Add(adjacentBlock);
            }
        }

        // Try to move all together
        foreach (var block in toMove)
        {
            if (!MoveBlock(block, direction)) return false;
        }

        return true;
    }
}