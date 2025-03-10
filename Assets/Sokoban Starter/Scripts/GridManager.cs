using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    private Dictionary<Vector2Int, GridObject> gridObjects = new Dictionary<Vector2Int, GridObject>();
    private GridObject player;

    private int gridWidth = 10; // Width of the grid
    private int gridHeight = 5; // Height of the grid
    private Vector2Int gridOffset = new Vector2Int(1, 1); // Adjusting for player's starting position

    // Initialize the grid and its objects when the game starts
    private void Start()
    {
        InitializeGrid();
    }

    // Handle player input every frame
    private void Update()
    {
        HandlePlayerInput();
    }

    // Initialize the grid and add the objects to the grid
    private void InitializeGrid()
    {
        // Get all the GridObject components in the scene
        GridObject[] objects = FindObjectsByType<GridObject>(FindObjectsSortMode.None);

        foreach (var obj in objects)
        {
            // Adjust each object's grid position based on the gridOffset
            obj.gridPosition += gridOffset;
            if (!gridObjects.ContainsKey(obj.gridPosition))
            {
                gridObjects[obj.gridPosition] = obj; // Add the object to the grid
            }

            // If this object is the player, store a reference to it
            if (obj.CompareTag("Player"))
            {
                player = obj;
            }
        }
    }

    // Handle the input for moving the player
    private void HandlePlayerInput()
    {
        Vector2Int direction = Vector2Int.zero;

        // Check for WASD keypresses and assign the corresponding direction
        if (Input.GetKeyDown(KeyCode.W)) direction = Vector2Int.down;  // Move up
        if (Input.GetKeyDown(KeyCode.S)) direction = Vector2Int.up; // Move down
        if (Input.GetKeyDown(KeyCode.A)) direction = Vector2Int.left; // Move left
        if (Input.GetKeyDown(KeyCode.D)) direction = Vector2Int.right; // Move right

        if (direction != Vector2Int.zero)
        {
            MoveBlock(player, direction); // Move the player if a valid direction is detected
        }
    }

    // Try to move a block (player or other objects) in the given direction
    private bool MoveBlock(GridObject block, Vector2Int direction)
    {
        Vector2Int newPosition = block.gridPosition + direction; // Calculate new position based on direction

        // Boundary check: Ensure the block stays within the grid bounds
        if (!IsWithinBounds(newPosition)) return false;

        // Check for collisions at the new position
        if (gridObjects.TryGetValue(newPosition, out GridObject other))
        {
            // If the block is colliding with a wall, don't allow movement
            if (other.CompareTag("Wall")) return false;
            // If the block is a Clingy object, handle the collision with it
            if (other.CompareTag("Clingy")) return HandleClingyCollision(block, other, direction);
        }

        // Prevent Clingy block from moving onto the player's position
        if (block.CompareTag("Clingy") && newPosition == player.gridPosition)
        {
            // If the Clingy block is adjacent to the player, move it to an empty adjacent position
            Vector2Int[] adjacentDirections = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            foreach (Vector2Int adjDir in adjacentDirections)
            {
                Vector2Int adjPos = player.gridPosition + adjDir;
                if (IsWithinBounds(adjPos) && !gridObjects.ContainsKey(adjPos))
                {
                    newPosition = adjPos; // Move Clingy block to empty adjacent space
                    break;
                }
            }
        }

        // Move the block to the new position
        UpdateBlockPosition(block, newPosition);
        CheckForClingyPull(block); // After moving the block, check if any Clingy block should be pulled

        // After moving the player, handle the movement of sticky and other blocks
        MoveBlocks(direction); // Move adjacent sticky and smooth blocks

        return true;
    }

    // Move sticky and smooth blocks after the player moves
    private void MoveBlocks(Vector2Int direction)
    {
        bool hasMoved = true;

        // Keep checking until no more blocks need to be moved
        while (hasMoved)
        {
            hasMoved = false;
            List<GridObject> movingObjects = new List<GridObject>();

            // Loop through all objects on the grid
            foreach (var kvp in gridObjects)
            {
                Vector2Int pos = kvp.Key;
                GridObject obj = kvp.Value;

                if (obj != null)
                {
                    // Handle sticky blocks movement
                    if (obj.CompareTag("Sticky"))
                    {
                        if (CheckSticky(pos, movingObjects) && CheckCollisions(pos, direction, movingObjects))
                        {
                            movingObjects.Add(obj); // Add sticky block to the move list
                            hasMoved = true;
                        }
                    }
                    // Handle Clingy blocks pulling behavior
                    else if (obj.CompareTag("Clingy"))
                    {
                        Vector2Int checkPos = pos + direction;
                        if (gridObjects.TryGetValue(checkPos, out GridObject adjObj) && (adjObj.CompareTag("Sticky") || adjObj.CompareTag("Player")) && CheckCollisions(pos, direction, movingObjects))
                        {
                            movingObjects.Add(obj); // Add Clingy block to the move list
                            hasMoved = true;
                        }
                    }
                    // Handle smooth blocks movement
                    else if (obj.CompareTag("Smooth"))
                    {
                        if (CheckCollisions(obj.gridPosition, direction, movingObjects) && IsPlayerPushingSmooth(obj, direction, movingObjects))
                        {
                            movingObjects.Add(obj); // Add smooth block to the move list
                            hasMoved = true;
                        }
                    }
                }
            }

            // Move all the blocks in the movingObjects list
            foreach (GridObject block in movingObjects)
            {
                Vector2Int newPosition = block.gridPosition + direction;

                // Ensure the new position is within bounds before moving
                if (IsWithinBounds(newPosition))
                {
                    gridObjects.Remove(block.gridPosition); // Remove block from old position
                    block.gridPosition = newPosition; // Update the block's position
                    gridObjects[newPosition] = block; // Add block to new position
                }
            }
        }
    }

    // Check if a block can be pushed by the player based on its type and direction
    private bool IsPlayerPushingSmooth(GridObject smooth, Vector2Int direction, List<GridObject> movingObjects)
    {
        Vector2Int checkPosition = smooth.gridPosition - direction; // Position behind the Smooth block

        // Check if the player is at the position behind the smooth block
        if (gridObjects.TryGetValue(checkPosition, out GridObject pusher) && pusher.CompareTag("Player"))
        {
            return true; // Player is pushing the smooth block
        }

        return false;
    }

    // Check if a sticky block should move based on its adjacent objects
    private bool CheckSticky(Vector2Int position, List<GridObject> movingObjects)
    {
        Vector2Int[] adjacentDirections = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        // Check each adjacent direction for other sticky blocks or the player
        foreach (Vector2Int adjDirection in adjacentDirections)
        {
            Vector2Int adjPos = position + adjDirection;
            if (gridObjects.TryGetValue(adjPos, out GridObject adjBlock))
            {
                if (adjBlock.CompareTag("Sticky"))
                {
                    // Sticky blocks that are connected will move together
                    if (movingObjects.Contains(adjBlock) || gridObjects.TryGetValue(position - adjDirection, out GridObject playerCheck) && playerCheck.CompareTag("Player"))
                    {
                        return true;
                    }
                }
                else if (adjBlock.CompareTag("Player"))
                {
                    return true; // The player is adjacent, so the sticky block will move
                }
            }
        }
        return false;
    }

    // Check if a block can move in the given direction
    private bool CheckCollisions(Vector2Int position, Vector2Int direction, List<GridObject> movingObjects)
    {
        Vector2Int newPosition = position + direction;
        if (gridObjects.TryGetValue(newPosition, out GridObject other))
        {
            // Check if the block is colliding with a wall or another block that prevents movement
            if (other.CompareTag("Wall") || other.CompareTag("Clingy"))
            {
                return false; // Block can't move due to collision
            }

            // Prevent sticky blocks from moving onto each other
            if (other.CompareTag("Sticky") && !movingObjects.Contains(other))
            {
                return false; // Block can't move because of sticky block
            }
        }
        return true; // No collision, movement is allowed
    }

    // After moving a block, check if any Clingy block should be pulled
    private void CheckForClingyPull(GridObject movedBlock)
    {
        Vector2Int[] adjacentDirections = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        foreach (Vector2Int adjDirection in adjacentDirections)
        {
            Vector2Int adjPosition = movedBlock.gridPosition + adjDirection;
            if (gridObjects.TryGetValue(adjPosition, out GridObject adjBlock) && adjBlock.CompareTag("Clingy"))
            {
                // If a Clingy block is adjacent to the moved block, pull it
                Vector2Int pullDirection = movedBlock.gridPosition - adjPosition;
                MoveBlock(adjBlock, pullDirection); // Pull the Clingy block
            }
        }
    }

    // Move a sticky block and its adjacent sticky blocks together
    private bool MoveSticky(GridObject sticky, Vector2Int direction)
    {
        // We need to track which blocks will move
        List<GridObject> toMove = new List<GridObject> { sticky };

        // Check each adjacent position to see if any other sticky block should be moved
        foreach (Vector2Int adj in new Vector2Int[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
        {
            Vector2Int adjPos = sticky.gridPosition + adj;

            // If the adjacent block is also sticky, add it to the list of blocks to move
            if (gridObjects.TryGetValue(adjPos, out GridObject adjacentBlock) && adjacentBlock.CompareTag("Sticky"))
            {
                toMove.Add(adjacentBlock); // Add the adjacent sticky block to the move list
            }
        }

        // Try to move all the sticky blocks in the list
        foreach (var block in toMove)
        {
            if (!CanMoveSticky(block, direction)) return false; // Check if the block can move

            // Try moving the block, if blocked, it won't move
            if (!MoveBlock(block, direction)) return false; // Move the block, return false if blocked
        }

        return true; // Successfully moved all sticky blocks
    }


    // Check if a sticky block can move in the given direction
    private bool CanMoveSticky(GridObject sticky, Vector2Int direction)
    {
        Vector2Int newPosition = sticky.gridPosition + direction;

        // Check for collisions with walls or Clingy blocks
        if (gridObjects.TryGetValue(newPosition, out GridObject other))
        {
            if (other.CompareTag("Wall") || other.CompareTag("Clingy"))
            {
                return false; // Block can't move due to collision
            }
        }

        return true; // The sticky block can move
    }

    // Move a smooth block in the given direction
    private bool MoveSmooth(GridObject smooth, Vector2Int direction)
    {
        Vector2Int newPosition = smooth.gridPosition + direction;

        // Check for collisions with walls or Clingy blocks
        if (gridObjects.TryGetValue(newPosition, out GridObject other))
        {
            if (other.CompareTag("Wall") || other.CompareTag("Clingy"))
            {
                return false; // Block can't move due to collision
            }
        }

        gridObjects.Remove(smooth.gridPosition); // Remove the block from its old position
        smooth.gridPosition = newPosition; // Update its position
        gridObjects[newPosition] = smooth; // Add it to the new position

        return true; // Block successfully moved
    }

    // Check if a position is within the grid's bounds
    private bool IsWithinBounds(Vector2Int position)
    {
        return position.x >= gridOffset.x && position.x < gridWidth + gridOffset.x &&
                position.y >= gridOffset.y && position.y < gridHeight + gridOffset.y;
    }

    // Handle collision with smooth blocks (not implemented fully here)
    private bool HandleSmoothCollision(GridObject mover, GridObject smooth, Vector2Int direction)
    {
        return MoveSmooth(smooth, direction);
    }

    // Handle collision with sticky blocks (not implemented fully here)
    private bool HandleStickyCollision(GridObject mover, GridObject sticky, Vector2Int direction)
    {
        return MoveSticky(sticky, direction);
    }

    // Handle collision with clingy blocks (they can't be pushed)
    private bool HandleClingyCollision(GridObject mover, GridObject clingy, Vector2Int direction)
    {
        return false; // Clingy blocks cannot be pushed
    }

    // Update a block's position on the grid
    private void UpdateBlockPosition(GridObject block, Vector2Int newPosition)
    {
        gridObjects.Remove(block.gridPosition); // Remove from old position
        block.gridPosition = newPosition; // Update block's position
        gridObjects[newPosition] = block; // Add block to new position
    }
}
