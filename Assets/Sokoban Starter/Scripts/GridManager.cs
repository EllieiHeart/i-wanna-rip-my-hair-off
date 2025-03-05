using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance;
    
    private Dictionary<Vector2Int, GameObject> occupiedCells = new Dictionary<Vector2Int, GameObject>();
    public int gridWidth = 10;
    public int gridHeight = 5; // Reflects your 10x5 grid

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public bool IsCellOccupied(Vector2Int cell) => occupiedCells.ContainsKey(cell);

    public bool IsWithinBounds(Vector2Int cell)
    {
        return cell.x >= 0 && cell.x < gridWidth && cell.y >= 0 && cell.y < gridHeight;
    }

    public void RegisterBlock(Vector2Int cell, GameObject block)
    {
        if (!occupiedCells.ContainsKey(cell))
            occupiedCells[cell] = block;
    }

    public void UnregisterBlock(Vector2Int cell)
    {
        if (occupiedCells.ContainsKey(cell))
            occupiedCells.Remove(cell);
    }
}
