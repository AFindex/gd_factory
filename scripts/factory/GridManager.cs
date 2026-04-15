using Godot;
using System.Collections.Generic;

public sealed class GridManager : IFactorySite
{
    private readonly Dictionary<Vector2I, GridReservation> _reservations = new();
    private readonly Dictionary<Vector2I, FactoryResourceDepositDefinition> _resourceCells = new();
    private readonly List<FactoryResourceDepositDefinition> _resourceDeposits = new();

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
    public int StructureRevision { get; private set; }
    public bool IsVisible => true;
    public bool IsSimulationActive => true;
    public float CombatOverlayScale => FactoryConstants.NormalCombatOverlayScale;
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

    public bool CanPlaceCells(IReadOnlyList<Vector2I> cells, string? ownerId = null)
    {
        for (var index = 0; index < cells.Count; index++)
        {
            if (!CanReserve(cells[index], ownerId))
            {
                return false;
            }
        }

        return true;
    }

    public bool CanPlaceStructure(BuildPrototypeKind kind, Vector2I cell, FacingDirection facing, out string reason)
    {
        var siteKind = FactoryIndustrialStandards.ResolveSiteKind(this);
        reason = string.Empty;
        if (!FactoryIndustrialStandards.IsStructureAllowed(kind, siteKind))
        {
            reason = FactoryIndustrialStandards.GetPlacementCompatibilityError(kind, siteKind);
            return false;
        }

        var footprintCells = FactoryPlacement.ResolveFootprintCells(kind, cell, facing);
        var matchedResourceCell = false;
        for (var index = 0; index < footprintCells.Count; index++)
        {
            var footprintCell = footprintCells[index];
            if (!CanReserve(footprintCell))
            {
                reason = $"格子 ({footprintCell.X}, {footprintCell.Y}) 已被现有结构占用。";
                return false;
            }

            if (_resourceCells.TryGetValue(footprintCell, out var deposit))
            {
                if (kind == BuildPrototypeKind.MiningDrill)
                {
                    if (!FactoryResourceCatalog.SupportsExtractor(kind, deposit.ResourceKind))
                    {
                        reason = $"{deposit.DisplayName} 不能由当前建筑开采。";
                        return false;
                    }

                    matchedResourceCell = true;
                    continue;
                }

                reason = $"{deposit.DisplayName} 上只能放置匹配的采矿机。";
                return false;
            }
        }

        if (kind == BuildPrototypeKind.MiningDrill && !matchedResourceCell)
        {
            reason = "采矿机必须放在矿点上。";
            return false;
        }

        return true;
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

    public IEnumerable<FactoryStructure> GetStructures()
    {
        var seen = new HashSet<ulong>();
        foreach (var reservation in _reservations.Values)
        {
            if (reservation.Structure is null)
            {
                continue;
            }

            if (seen.Add(reservation.Structure.GetInstanceId()))
            {
                yield return reservation.Structure;
            }
        }
    }

    public void SetResourceDeposits(IEnumerable<FactoryResourceDepositDefinition> deposits)
    {
        _resourceCells.Clear();
        _resourceDeposits.Clear();

        foreach (var deposit in deposits)
        {
            _resourceDeposits.Add(deposit);
            for (var index = 0; index < deposit.Cells.Count; index++)
            {
                _resourceCells[deposit.Cells[index]] = deposit;
            }
        }
    }

    public IReadOnlyList<FactoryResourceDepositDefinition> GetResourceDeposits()
    {
        return _resourceDeposits;
    }

    public bool TryGetResourceDeposit(Vector2I cell, out FactoryResourceDepositDefinition? deposit)
    {
        return _resourceCells.TryGetValue(cell, out deposit);
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
        var changed = false;
        foreach (var cell in cells)
        {
            _reservations[cell] = new GridReservation(ownerId, kind, structure);
            changed = true;
        }

        if (changed)
        {
            StructureRevision++;
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
