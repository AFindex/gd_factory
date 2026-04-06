using Godot;
using System.Collections.Generic;

public readonly struct FactoryPlacementValidationResult
{
    public FactoryPlacementValidationResult(bool isValid, IReadOnlyList<Vector2I> occupiedCells, string reason)
    {
        IsValid = isValid;
        OccupiedCells = occupiedCells;
        Reason = reason;
    }

    public bool IsValid { get; }
    public IReadOnlyList<Vector2I> OccupiedCells { get; }
    public string Reason { get; }
}

public static class FactoryPlacement
{
    public static IReadOnlyList<Vector2I> ResolveFootprintCells(BuildPrototypeKind kind, Vector2I anchorCell, FacingDirection facing)
    {
        var footprint = FactoryStructureFactory.GetFootprint(kind);
        var cells = new List<Vector2I>();
        foreach (var cell in footprint.ResolveOccupiedCells(anchorCell, facing))
        {
            cells.Add(cell);
        }

        return cells;
    }

    public static Vector3 GetPreviewCenter(IFactorySite site, BuildPrototypeKind kind, Vector2I anchorCell, FacingDirection facing)
    {
        var anchorWorld = site.CellToWorld(anchorCell);
        var centerOffset = FactoryStructureFactory.GetFootprint(kind).GetWorldCenterOffset(site.CellSize, facing);
        return anchorWorld + centerOffset.Rotated(Vector3.Up, site.WorldRotationRadians);
    }

    public static Vector2 GetPreviewSize(IFactorySite site, BuildPrototypeKind kind, FacingDirection facing)
    {
        return FactoryStructureFactory.GetFootprint(kind).GetPreviewSize(site.CellSize, facing);
    }

    public static Vector2 GetPreviewBaseSize(IFactorySite site, BuildPrototypeKind kind)
    {
        return FactoryStructureFactory.GetFootprint(kind).GetPreviewSize(site.CellSize, FacingDirection.East);
    }
}
