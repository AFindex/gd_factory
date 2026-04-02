using Godot;
using System.Collections.Generic;

public sealed class GridManager : IFactorySite
{
    private readonly Dictionary<Vector2I, GridReservation> _reservations = new();

    public GridManager(Vector2I minCell, Vector2I maxCell, float cellSize)
    {
        MinCell = minCell;
        MaxCell = maxCell;
        CellSize = cellSize;
    }

    public string SiteId => "world";
    public Vector2I MinCell { get; }
    public Vector2I MaxCell { get; }
    public float CellSize { get; }
    public bool IsVisible => true;
    public bool IsSimulationActive => true;
    public float WorldRotationRadians => 0.0f;

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
        return CanReserve(cell);
    }

    public bool CanReserve(Vector2I cell, string? ownerId = null)
    {
        if (!IsInBounds(cell))
        {
            return false;
        }

        return !_reservations.TryGetValue(cell, out var reservation) || reservation.OwnerId == ownerId;
    }

    public bool CanReserveAll(IEnumerable<Vector2I> cells, string ownerId)
    {
        foreach (var cell in cells)
        {
            if (!CanReserve(cell, ownerId))
            {
                return false;
            }
        }

        return true;
    }

    public bool TryGetStructure(Vector2I cell, out FactoryStructure? structure)
    {
        structure = null;

        if (!_reservations.TryGetValue(cell, out var reservation))
        {
            return false;
        }

        structure = reservation.Structure;
        return structure is not null;
    }

    public bool TryGetReservation(Vector2I cell, out GridReservation? reservation)
    {
        return _reservations.TryGetValue(cell, out reservation);
    }

    public void PlaceStructure(FactoryStructure structure)
    {
        ReserveCells(structure.GetOccupiedCells(), structure.ReservationOwnerId, GridReservationKind.StaticStructure, structure);
    }

    public void RemoveStructure(FactoryStructure structure)
    {
        ReleaseOwner(structure.ReservationOwnerId);
    }

    public void ReserveCells(IEnumerable<Vector2I> cells, string ownerId, GridReservationKind kind, FactoryStructure? structure = null)
    {
        foreach (var cell in cells)
        {
            _reservations[cell] = new GridReservation(ownerId, kind, structure);
        }
    }

    public void ReleaseOwner(string ownerId)
    {
        var toRemove = new List<Vector2I>();
        foreach (var pair in _reservations)
        {
            if (pair.Value.OwnerId == ownerId)
            {
                toRemove.Add(pair.Key);
            }
        }

        foreach (var cell in toRemove)
        {
            _reservations.Remove(cell);
        }
    }

    public bool TrySendItem(FactoryStructure source, Vector2I targetCell, FactoryItem item, SimulationController simulation)
    {
        if (!TryGetStructure(targetCell, out var structure) || structure is null)
        {
            return false;
        }

        return structure.TryAcceptItem(item, source.Cell, simulation);
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
