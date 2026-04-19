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
        return ResolveFootprintCells(kind, anchorCell, facing, configuration: null, mapRecipeId: null);
    }

    public static IReadOnlyList<Vector2I> ResolveFootprintCells(
        BuildPrototypeKind kind,
        Vector2I anchorCell,
        FacingDirection facing,
        IReadOnlyDictionary<string, string>? configuration,
        string? mapRecipeId = null)
    {
        return FactoryStructureLogisticsContractResolver.Resolve(kind, anchorCell, facing, configuration, mapRecipeId).OccupiedCells;
    }

    public static Vector3 GetPreviewCenter(IFactorySite site, BuildPrototypeKind kind, Vector2I anchorCell, FacingDirection facing)
    {
        var anchorWorld = site.CellToWorld(anchorCell);
        var contract = FactoryStructureLogisticsContractResolver.Resolve(kind, anchorCell, facing);
        var centerOffset = contract.GetWorldCenterOffset(site.CellSize);
        return anchorWorld + centerOffset.Rotated(Vector3.Up, site.WorldRotationRadians);
    }

    public static Vector2 GetPreviewSize(IFactorySite site, BuildPrototypeKind kind, FacingDirection facing)
    {
        return FactoryStructureLogisticsContractResolver.Resolve(kind, Vector2I.Zero, facing).GetPreviewSize(site.CellSize);
    }

    public static Vector2 GetPreviewBaseSize(IFactorySite site, BuildPrototypeKind kind)
    {
        return FactoryStructureLogisticsContractResolver.Resolve(kind, Vector2I.Zero, FacingDirection.East).GetPreviewSize(site.CellSize);
    }
}
