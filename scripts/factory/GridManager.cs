using Godot;
using System.Collections.Generic;

public sealed class GridManager
{
    private readonly Dictionary<Vector2I, FactoryStructure> _occupiedCells = new();

    public GridManager(Vector2I minCell, Vector2I maxCell, float cellSize)
    {
        MinCell = minCell;
        MaxCell = maxCell;
        CellSize = cellSize;
    }

    public Vector2I MinCell { get; }
    public Vector2I MaxCell { get; }
    public float CellSize { get; }

    public bool IsInBounds(Vector2I cell)
    {
        return cell.X >= MinCell.X && cell.X <= MaxCell.X && cell.Y >= MinCell.Y && cell.Y <= MaxCell.Y;
    }

    public Vector2I WorldToCell(Vector3 worldPosition)
    {
        return new Vector2I(
            Mathf.RoundToInt(worldPosition.X / CellSize),
            Mathf.RoundToInt(worldPosition.Z / CellSize));
    }

    public Vector3 CellToWorld(Vector2I cell)
    {
        return new Vector3(cell.X * CellSize, 0.0f, cell.Y * CellSize);
    }

    public bool CanPlace(Vector2I cell)
    {
        return IsInBounds(cell) && !_occupiedCells.ContainsKey(cell);
    }

    public bool TryGetStructure(Vector2I cell, out FactoryStructure? structure)
    {
        return _occupiedCells.TryGetValue(cell, out structure);
    }

    public void PlaceStructure(FactoryStructure structure)
    {
        _occupiedCells[structure.Cell] = structure;
    }

    public void RemoveStructure(FactoryStructure structure)
    {
        if (_occupiedCells.TryGetValue(structure.Cell, out var existing) && existing == structure)
        {
            _occupiedCells.Remove(structure.Cell);
        }
    }

    public Vector2 GetWorldMin()
    {
        return new Vector2(
            (MinCell.X - 0.5f) * CellSize,
            (MinCell.Y - 0.5f) * CellSize);
    }

    public Vector2 GetWorldMax()
    {
        return new Vector2(
            (MaxCell.X + 0.5f) * CellSize,
            (MaxCell.Y + 0.5f) * CellSize);
    }
}
