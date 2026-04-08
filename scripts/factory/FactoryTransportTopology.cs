using Godot;
using System.Collections.Generic;

public static class FactoryTransportTopology
{
    public static IReadOnlyList<Vector2I> GetInputCells(FactoryStructure structure)
    {
        return GetInputCells(structure.Kind, structure.Cell, structure.Facing);
    }

    public static IReadOnlyList<Vector2I> GetOutputCells(FactoryStructure structure)
    {
        return GetOutputCells(structure.Kind, structure.Cell, structure.Facing);
    }

    public static IReadOnlyList<Vector2I> GetInputCells(BuildPrototypeKind kind, Vector2I cell, FacingDirection facing)
    {
        return kind switch
        {
            BuildPrototypeKind.Belt => GetBeltInputCells(cell, facing),
            BuildPrototypeKind.Merger => GetMergerInputCells(cell, facing),
            _ => FactoryStructureFactory.GetFootprint(kind).ResolveInputCells(cell, facing)
        };
    }

    public static IReadOnlyList<Vector2I> GetOutputCells(BuildPrototypeKind kind, Vector2I cell, FacingDirection facing)
    {
        return FactoryStructureFactory.GetFootprint(kind).ResolveOutputCells(cell, facing);
    }

    public static IReadOnlyList<Vector2I> GetBeltInputCells(Vector2I cell, FacingDirection facing)
    {
        return new[]
        {
            cell + FactoryDirection.ToCellOffset(FactoryDirection.Opposite(facing)),
            cell + FactoryDirection.ToCellOffset(FactoryDirection.RotateCounterClockwise(facing)),
            cell + FactoryDirection.ToCellOffset(FactoryDirection.RotateClockwise(facing))
        };
    }

    public static Vector2I GetBeltOutputCell(Vector2I cell, FacingDirection facing)
    {
        return cell + FactoryDirection.ToCellOffset(facing);
    }

    public static bool BeltCanReceiveFrom(IFactorySite site, Vector2I cell, FacingDirection facing, Vector2I sourceCell)
    {
        if (!IsOrthogonallyAdjacent(cell, sourceCell) || sourceCell == GetBeltOutputCell(cell, facing))
        {
            return false;
        }

        return site.TryGetStructure(sourceCell, out var structure)
            && structure is not null
            && structure.CanOutputTo(cell);
    }

    public static FacingDirection DetermineBeltPrimaryInputFacing(IFactorySite site, Vector2I cell, FacingDirection facing)
    {
        var preferredDirections = new[]
        {
            FactoryDirection.Opposite(facing),
            FactoryDirection.RotateCounterClockwise(facing),
            FactoryDirection.RotateClockwise(facing)
        };

        for (var index = 0; index < preferredDirections.Length; index++)
        {
            var candidateCell = cell + FactoryDirection.ToCellOffset(preferredDirections[index]);
            if (BeltCanReceiveFrom(site, cell, facing, candidateCell))
            {
                return preferredDirections[index];
            }
        }

        return FactoryDirection.Opposite(facing);
    }

    public static bool TryGetBeltMidspanMergeTarget(IFactorySite site, Vector2I cell, FacingDirection facing, out Vector2I targetCell)
    {
        targetCell = GetBeltOutputCell(cell, facing);
        return site.TryGetStructure(targetCell, out var structure) && structure is BeltStructure;
    }

    public static IReadOnlyList<Vector2I> GetMergerInputCells(Vector2I cell, FacingDirection facing)
    {
        return new[]
        {
            cell + FactoryDirection.ToCellOffset(FactoryDirection.Opposite(facing)),
            cell + FactoryDirection.ToCellOffset(FactoryDirection.RotateCounterClockwise(facing)),
            cell + FactoryDirection.ToCellOffset(FactoryDirection.RotateClockwise(facing))
        };
    }

    public static bool MergerCanReceiveFrom(Vector2I cell, FacingDirection facing, Vector2I sourceCell)
    {
        var inputCells = GetMergerInputCells(cell, facing);
        for (var index = 0; index < inputCells.Count; index++)
        {
            if (inputCells[index] == sourceCell)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsOrthogonallyAdjacent(Vector2I a, Vector2I b)
    {
        var delta = a - b;
        return Mathf.Abs(delta.X) + Mathf.Abs(delta.Y) == 1;
    }
}
