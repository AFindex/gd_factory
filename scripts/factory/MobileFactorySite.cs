using Godot;
using System.Collections.Generic;

public sealed class MobileFactorySite : IFactorySite
{
    private readonly Dictionary<Vector2I, FactoryStructure> _structures = new();
    private Vector3 _worldOrigin;
    private float _worldRotationRadians;

    public MobileFactorySite(string siteId, Vector2I minCell, Vector2I maxCell, float cellSize, MobileFactoryInstance owner)
    {
        SiteId = siteId;
        MinCell = minCell;
        MaxCell = maxCell;
        CellSize = cellSize;
        Owner = owner;
        IsVisible = false;
        IsSimulationActive = false;
    }

    public string SiteId { get; }
    public MobileFactoryInstance Owner { get; }
    public Vector2I MinCell { get; }
    public Vector2I MaxCell { get; }
    public float CellSize { get; }
    public int StructureRevision { get; private set; }
    public bool IsVisible { get; private set; }
    public bool IsSimulationActive { get; private set; }
    public float CombatOverlayScale { get; private set; } = FactoryConstants.MobileInteriorCombatOverlayScale;
    public Vector3 WorldOrigin => _worldOrigin;
    public float WorldRotationRadians => _worldRotationRadians;

    public bool IsInBounds(Vector2I cell)
    {
        return cell.X >= MinCell.X && cell.X <= MaxCell.X && cell.Y >= MinCell.Y && cell.Y <= MaxCell.Y;
    }

    public bool CanPlace(Vector2I cell)
    {
        return IsInBounds(cell) && !_structures.ContainsKey(cell);
    }

    public bool CanPlaceCells(IReadOnlyList<Vector2I> cells, string? ownerId = null)
    {
        for (var index = 0; index < cells.Count; index++)
        {
            var cell = cells[index];
            if (!IsInBounds(cell))
            {
                return false;
            }

            if (_structures.TryGetValue(cell, out var structure) && (ownerId is null || structure.ReservationOwnerId != ownerId))
            {
                return false;
            }
        }

        return true;
    }

    public Vector3 CellToWorld(Vector2I cell)
    {
        var local = new Vector3(cell.X * CellSize, 0.0f, cell.Y * CellSize);
        return _worldOrigin + local.Rotated(Vector3.Up, _worldRotationRadians);
    }

    public Vector2I WorldToCell(Vector3 worldPosition)
    {
        var local = (worldPosition - _worldOrigin).Rotated(Vector3.Up, -_worldRotationRadians);
        return new Vector2I(
            Mathf.RoundToInt(local.X / CellSize),
            Mathf.RoundToInt(local.Z / CellSize));
    }

    public bool TryGetStructure(Vector2I cell, out FactoryStructure? structure)
    {
        return _structures.TryGetValue(cell, out structure);
    }

    public IEnumerable<FactoryStructure> GetStructures()
    {
        var seen = new HashSet<ulong>();
        foreach (var structure in _structures.Values)
        {
            if (seen.Add(structure.GetInstanceId()))
            {
                yield return structure;
            }
        }
    }

    public void AddStructure(FactoryStructure structure)
    {
        foreach (var cell in structure.GetOccupiedCells())
        {
            _structures[cell] = structure;
        }

        StructureRevision++;
    }

    public void RemoveStructure(FactoryStructure structure)
    {
        var toRemove = new List<Vector2I>();
        foreach (var pair in _structures)
        {
            if (pair.Value == structure)
            {
                toRemove.Add(pair.Key);
            }
        }

        for (var index = 0; index < toRemove.Count; index++)
        {
            _structures.Remove(toRemove[index]);
        }

        if (toRemove.Count > 0)
        {
            StructureRevision++;
        }
    }

    public bool TrySendItem(FactoryStructure source, Vector2I sourceCell, Vector2I targetCell, FactoryItem item, SimulationController simulation)
    {
        if (!FactoryStructurePortResolver.TryResolveReceiver(this, targetCell, out var resolution))
        {
            return false;
        }

        var effectiveSourceCell = resolution.ResolveEffectiveSourceCell(sourceCell, targetCell);
        return resolution.Structure.TryAcceptItem(item, effectiveSourceCell, simulation);
    }

    public void SetWorldTransform(Vector3 worldOrigin, float worldRotationRadians)
    {
        _worldOrigin = worldOrigin;
        _worldRotationRadians = worldRotationRadians;
        RefreshStructures();
    }

    public void SetRuntimeState(bool isVisible, bool isSimulationActive)
    {
        IsVisible = isVisible;
        IsSimulationActive = isSimulationActive;
        RefreshStructures();
    }

    public void SetCombatOverlayScale(float combatOverlayScale)
    {
        CombatOverlayScale = Mathf.Max(0.1f, combatOverlayScale);
        RefreshStructures();
    }

    private void RefreshStructures()
    {
        foreach (var structure in _structures.Values)
        {
            structure.RefreshPlacement();
        }
    }
}
