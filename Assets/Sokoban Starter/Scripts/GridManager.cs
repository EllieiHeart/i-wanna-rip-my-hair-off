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

        // Boundary check
        if (!IsWithinBounds(newPosition)) return false;

        // Collision check
        if (gridObjects.TryGetValue(newPosition, out GridObject other))
        {
            if (other.CompareTag("Wall")) return false;
            if (other.CompareTag("Clingy")) return HandleClingyCollision(block, other, direction);
        }

        // Move block
        UpdateBlockPosition(block, newPosition);
        CheckForClingyPull(block); // Check for Clingy pull after movement

        // Handle smooth and sticky movements after player moves.
        MoveBlocks(direction); // Use the modified moveBlocks method

        return true;
    }

    private void MoveBlocks(Vector2Int direction)
    {
        bool hasMoved = true;

        while (hasMoved)
        {
            hasMoved = false;
            List<GridObject> movingObjects = new List<GridObject>();

            foreach (var kvp in gridObjects)
            {
                Vector2Int pos = kvp.Key;
                GridObject obj = kvp.Value;

                if (obj != null)
                {
                    if (obj.CompareTag("Sticky"))
                    {
                        if (CheckSticky(pos, movingObjects) && CheckCollisions(pos, direction, movingObjects))
                        {
                            movingObjects.Add(obj);
                            hasMoved = true;
                        }
                    }
                    else if (obj.CompareTag("Clingy"))
                    {
                        Vector2Int checkPos = pos + direction;
                        if (gridObjects.TryGetValue(checkPos, out GridObject adjObj) && (adjObj.CompareTag("Sticky") || adjObj.CompareTag("Player")) && CheckCollisions(pos, direction, movingObjects))
                        {
                            movingObjects.Add(obj);
                            hasMoved = true;
                        }
                    }
                    else if (obj.CompareTag("Smooth"))
                    {
                        if (IsPlayerPushingSmooth(obj, direction, movingObjects) && CheckCollisions(obj.gridPosition, direction, movingObjects))
                        {
                            movingObjects.Add(obj);
                            hasMoved = true;
                        }
                    }
                }
            }


            foreach (GridObject block in movingObjects)
            {
                Vector2Int newPosition = block.gridPosition + direction;

                // Boundary check before moving the block
                if (IsWithinBounds(newPosition))
                {
                    gridObjects.Remove(block.gridPosition); // Remove only if within bounds
                    block.gridPosition = newPosition;
                    gridObjects[newPosition] = block;
                }
                // Add error handling or logging here if needed.
            }
        }
    }

    private bool IsPlayerPushingSmooth(GridObject smooth, Vector2Int direction, List<GridObject> movingObjects)
    {
        Vector2Int checkPosition = smooth.gridPosition - direction;

        if (gridObjects.TryGetValue(checkPosition, out GridObject pusher))
        {
            if (pusher.CompareTag("Player") || (pusher.CompareTag("Smooth") && movingObjects.Contains(pusher)) || (pusher.CompareTag("Sticky") && movingObjects.Contains(pusher)))
            {
                return true;
            }
        }
        return false;
    }

    private bool CheckSticky(Vector2Int position, List<GridObject> movingObjects)
    {
        Vector2Int[] adjacentDirections = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        foreach (Vector2Int adjDirection in adjacentDirections)
        {
            Vector2Int adjPos = position + adjDirection;
            if (gridObjects.TryGetValue(adjPos, out GridObject adjBlock))
            {
                if (adjBlock.CompareTag("Sticky"))
                {
                    // Check if the adjacent sticky block is the player or should be moving
                    if (movingObjects.Contains(adjBlock) || gridObjects.TryGetValue(position - adjDirection, out GridObject playerCheck) && playerCheck.CompareTag("Player"))
                    {
                        return true;
                    }
                }
                else if (adjBlock.CompareTag("Player"))
                {
                    return true;
                }
            }
        }
        return false;
    }

    private bool CheckCollisions(Vector2Int position, Vector2Int direction, List<GridObject> movingObjects)
    {
        Vector2Int newPosition = position + direction;
        if (gridObjects.TryGetValue(newPosition, out GridObject other))
        {
            if (other.CompareTag("Wall") || other.CompareTag("Clingy"))
            {
                return false;
            }

            if (other.CompareTag("Sticky") && !movingObjects.Contains(other))
            {
                return false;
            }
        }
        return true;
    }

    private void CheckForClingyPull(GridObject movedBlock)
    {
        Vector2Int[] adjacentDirections = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        foreach (Vector2Int adjDirection in adjacentDirections)
        {
            Vector2Int adjPosition = movedBlock.gridPosition + adjDirection;
            if (gridObjects.TryGetValue(adjPosition, out GridObject adjBlock) && adjBlock.CompareTag("Clingy"))
            {
                Vector2Int pullDirection = movedBlock.gridPosition - adjPosition;
                MoveBlock(adjBlock, pullDirection); // Pull the Clingy block
            }
        }
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
            // If Sticky cannot move, return false
            if (!CanMoveSticky(block, direction)) return false;

            // Move sticky
            if (!MoveBlock(block, direction)) return false;
        }

        return true;
    }

    private bool CanMoveSticky(GridObject sticky, Vector2Int direction)
    {
        Vector2Int newPosition = sticky.gridPosition + direction;

        // Check if the new position is blocked (like a wall or other non-sticky block)
        if (gridObjects.TryGetValue(newPosition, out GridObject other))
        {
            if (other.CompareTag("Wall") || other.CompareTag("Clingy"))
            {
                return false; // Blocked by wall or clingy
            }
        }

        return true;
    }

    private bool MoveSmooth(GridObject smooth, Vector2Int direction)
    {
        Vector2Int newPosition = smooth.gridPosition + direction;

        // Check if the new position is blocked by another block
        if (gridObjects.TryGetValue(newPosition, out GridObject other))
        {
            if (other.CompareTag("Wall") || other.CompareTag("Clingy"))
            {
                return false; // Blocked by wall or clingy
            }
        }

        // Move the smooth block to the new position
        // Update the smooth block's position in the grid
        gridObjects.Remove(smooth.gridPosition); // Remove the block from its old position
        smooth.gridPosition = newPosition; // Update the block's position
        gridObjects[newPosition] = smooth; // Add the block to the new position

        return true;
    }

    private bool IsWithinBounds(Vector2Int position)
    {
        return position.x >= gridOffset.x && position.x < gridWidth + gridOffset.x &&
                position.y >= gridOffset.y && position.y < gridHeight + gridOffset.y;
    }

    private bool HandleSmoothCollision(GridObject mover, GridObject smooth, Vector2Int direction)
    {
        return MoveSmooth(smooth, direction);
    }

    private bool HandleStickyCollision(GridObject mover, GridObject sticky, Vector2Int direction)
    {
        return MoveSticky(sticky, direction);
    }

    private bool HandleClingyCollision(GridObject mover, GridObject clingy, Vector2Int direction)
    {
        return false; // Clingy cannot be pushed
    }

    private void UpdateBlockPosition(GridObject block, Vector2Int newPosition)
    {
        gridObjects.Remove(block.gridPosition);
        block.gridPosition = newPosition;
        gridObjects[newPosition] = block;
    }


    private void CheckForStickyPush(GridObject pusher, Vector2Int direction)
    {
        Vector2Int[] adjacentDirections = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        foreach (Vector2Int adjDirection in adjacentDirections)
        {
            Vector2Int adjPosition = pusher.gridPosition + adjDirection;
            if (gridObjects.TryGetValue(adjPosition, out GridObject adjBlock) && adjBlock.CompareTag("Sticky"))
            {
                MoveSticky(adjBlock, direction); // Push the sticky block
            }
        }
    }
}