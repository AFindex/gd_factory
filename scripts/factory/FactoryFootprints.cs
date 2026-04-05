using Godot;
using System.Collections.Generic;

public sealed class FactoryStructureFootprint
{
    public static readonly FactoryStructureFootprint SingleCell = new(
        new[] { Vector2I.Zero });

    public FactoryStructureFootprint(
        IReadOnlyList<Vector2I> occupiedOffsetsEast,
        Vector2I? inputOffsetEast = null,
        Vector2I? outputOffsetEast = null)
    {
        OccupiedOffsetsEast = occupiedOffsetsEast.Count > 0
            ? occupiedOffsetsEast
            : new[] { Vector2I.Zero };
        InputOffsetEast = inputOffsetEast;
        OutputOffsetEast = outputOffsetEast;
    }

    public IReadOnlyList<Vector2I> OccupiedOffsetsEast { get; }
    public Vector2I? InputOffsetEast { get; }
    public Vector2I? OutputOffsetEast { get; }

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

    public Vector2I ResolveInputCell(Vector2I anchorCell, FacingDirection facing)
    {
        if (InputOffsetEast is Vector2I inputOffset)
        {
            return anchorCell + FactoryDirection.RotateOffset(inputOffset, facing);
        }

        return anchorCell - FactoryDirection.ToCellOffset(facing);
    }

    public Vector2I ResolveOutputCell(Vector2I anchorCell, FacingDirection facing)
    {
        if (OutputOffsetEast is Vector2I outputOffset)
        {
            return anchorCell + FactoryDirection.RotateOffset(outputOffset, facing);
        }

        return anchorCell + FactoryDirection.ToCellOffset(facing);
    }
}
