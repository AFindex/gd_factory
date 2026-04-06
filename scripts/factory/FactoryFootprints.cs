using Godot;
using System.Collections.Generic;

public sealed class FactoryStructureFootprint
{
    public static readonly FactoryStructureFootprint SingleCell = new(
        new[] { Vector2I.Zero });

    public FactoryStructureFootprint(
        IReadOnlyList<Vector2I> occupiedOffsetsEast,
        Vector2I? inputOffsetEast = null,
        Vector2I? outputOffsetEast = null,
        IReadOnlyList<Vector2I>? inputOffsetsEast = null,
        IReadOnlyList<Vector2I>? outputOffsetsEast = null)
    {
        OccupiedOffsetsEast = occupiedOffsetsEast.Count > 0
            ? occupiedOffsetsEast
            : new[] { Vector2I.Zero };
        InputOffsetsEast = BuildPortOffsets(inputOffsetsEast, inputOffsetEast);
        OutputOffsetsEast = BuildPortOffsets(outputOffsetsEast, outputOffsetEast);
    }

    public IReadOnlyList<Vector2I> OccupiedOffsetsEast { get; }
    public IReadOnlyList<Vector2I> InputOffsetsEast { get; }
    public IReadOnlyList<Vector2I> OutputOffsetsEast { get; }

    public IReadOnlyList<Vector2I> ResolveOccupiedOffsets(FacingDirection facing)
    {
        var resolved = new Vector2I[OccupiedOffsetsEast.Count];
        for (var index = 0; index < OccupiedOffsetsEast.Count; index++)
        {
            resolved[index] = FactoryDirection.RotateOffset(OccupiedOffsetsEast[index], facing);
        }

        return resolved;
    }

    public IEnumerable<Vector2I> ResolveOccupiedCells(Vector2I anchorCell, FacingDirection facing)
    {
        for (var index = 0; index < OccupiedOffsetsEast.Count; index++)
        {
            yield return anchorCell + FactoryDirection.RotateOffset(OccupiedOffsetsEast[index], facing);
        }
    }

    public Rect2I GetRotatedBounds(FacingDirection facing)
    {
        var offsets = ResolveOccupiedOffsets(facing);
        var minX = offsets[0].X;
        var minY = offsets[0].Y;
        var maxX = offsets[0].X;
        var maxY = offsets[0].Y;

        for (var index = 1; index < offsets.Count; index++)
        {
            var cell = offsets[index];
            minX = Mathf.Min(minX, cell.X);
            minY = Mathf.Min(minY, cell.Y);
            maxX = Mathf.Max(maxX, cell.X);
            maxY = Mathf.Max(maxY, cell.Y);
        }

        return new Rect2I(minX, minY, maxX - minX + 1, maxY - minY + 1);
    }

    public Vector3 GetWorldCenterOffset(float cellSize, FacingDirection facing)
    {
        var offsets = ResolveOccupiedOffsets(facing);
        var sum = Vector2.Zero;
        for (var index = 0; index < offsets.Count; index++)
        {
            sum += new Vector2(offsets[index].X, offsets[index].Y);
        }

        var average = sum / offsets.Count;
        return new Vector3(average.X * cellSize, 0.0f, average.Y * cellSize);
    }

    public Vector2 GetPreviewSize(float cellSize, FacingDirection facing)
    {
        var bounds = GetRotatedBounds(facing);
        return new Vector2(bounds.Size.X * cellSize, bounds.Size.Y * cellSize);
    }

    public float GetCombatRadius(float cellSize, FacingDirection facing)
    {
        var size = GetPreviewSize(cellSize, facing);
        return Mathf.Max(size.X, size.Y) * 0.5f;
    }

    public Vector2 GetOccupiedCenterOffsetEast()
    {
        var sum = Vector2.Zero;
        for (var index = 0; index < OccupiedOffsetsEast.Count; index++)
        {
            sum += new Vector2(OccupiedOffsetsEast[index].X, OccupiedOffsetsEast[index].Y);
        }

        return sum / OccupiedOffsetsEast.Count;
    }

    public Vector3 GetLocalCellCenterOffset(Vector2I offsetEast, float cellSize)
    {
        var center = GetOccupiedCenterOffsetEast();
        return new Vector3(
            (offsetEast.X - center.X) * cellSize,
            0.0f,
            (offsetEast.Y - center.Y) * cellSize);
    }

    public IReadOnlyList<Vector2I> ResolveInputOffsets(FacingDirection facing)
    {
        return ResolvePortOffsets(InputOffsetsEast, facing, -FactoryDirection.ToCellOffset(facing));
    }

    public IReadOnlyList<Vector2I> ResolveOutputOffsets(FacingDirection facing)
    {
        return ResolvePortOffsets(OutputOffsetsEast, facing, FactoryDirection.ToCellOffset(facing));
    }

    public Vector2I ResolveInputCell(Vector2I anchorCell, FacingDirection facing)
    {
        return ResolveInputCells(anchorCell, facing)[0];
    }

    public Vector2I ResolveOutputCell(Vector2I anchorCell, FacingDirection facing)
    {
        return ResolveOutputCells(anchorCell, facing)[0];
    }

    public IReadOnlyList<Vector2I> ResolveInputCells(Vector2I anchorCell, FacingDirection facing)
    {
        var offsets = ResolveInputOffsets(facing);
        var cells = new Vector2I[offsets.Count];
        for (var index = 0; index < offsets.Count; index++)
        {
            cells[index] = anchorCell + offsets[index];
        }

        return cells;
    }

    public IReadOnlyList<Vector2I> ResolveOutputCells(Vector2I anchorCell, FacingDirection facing)
    {
        var offsets = ResolveOutputOffsets(facing);
        var cells = new Vector2I[offsets.Count];
        for (var index = 0; index < offsets.Count; index++)
        {
            cells[index] = anchorCell + offsets[index];
        }

        return cells;
    }

    public Vector2I ResolveOutputTransferCell(Vector2I anchorCell, FacingDirection facing, Vector2I targetCell)
    {
        var outputOffsets = ResolveOutputOffsets(facing);
        var occupiedOffsets = ResolveOccupiedOffsets(facing);
        for (var outputIndex = 0; outputIndex < outputOffsets.Count; outputIndex++)
        {
            if (anchorCell + outputOffsets[outputIndex] != targetCell)
            {
                continue;
            }

            for (var occupiedIndex = 0; occupiedIndex < occupiedOffsets.Count; occupiedIndex++)
            {
                if (!IsOrthogonallyAdjacent(outputOffsets[outputIndex], occupiedOffsets[occupiedIndex]))
                {
                    continue;
                }

                return anchorCell + occupiedOffsets[occupiedIndex];
            }
        }

        return anchorCell;
    }

    private static IReadOnlyList<Vector2I> BuildPortOffsets(IReadOnlyList<Vector2I>? preferredOffsets, Vector2I? fallbackOffset)
    {
        if (preferredOffsets is not null && preferredOffsets.Count > 0)
        {
            return preferredOffsets;
        }

        if (fallbackOffset is Vector2I singleOffset)
        {
            return new[] { singleOffset };
        }

        return System.Array.Empty<Vector2I>();
    }

    private static IReadOnlyList<Vector2I> ResolvePortOffsets(IReadOnlyList<Vector2I> eastOffsets, FacingDirection facing, Vector2I fallbackOffset)
    {
        if (eastOffsets.Count == 0)
        {
            return new[] { fallbackOffset };
        }

        var resolved = new Vector2I[eastOffsets.Count];
        for (var index = 0; index < eastOffsets.Count; index++)
        {
            resolved[index] = FactoryDirection.RotateOffset(eastOffsets[index], facing);
        }

        return resolved;
    }

    private static bool IsOrthogonallyAdjacent(Vector2I a, Vector2I b)
    {
        var delta = a - b;
        return Mathf.Abs(delta.X) + Mathf.Abs(delta.Y) == 1;
    }
}
