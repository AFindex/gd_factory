using Godot;
using System;
using System.Collections.Generic;

public static class FactorySelectionRectSupport
{
    public static Rect2I BuildInclusiveRect(Vector2I start, Vector2I end)
    {
        var minX = Mathf.Min(start.X, end.X);
        var minY = Mathf.Min(start.Y, end.Y);
        var maxX = Mathf.Max(start.X, end.X);
        var maxY = Mathf.Max(start.Y, end.Y);
        return new Rect2I(minX, minY, maxX - minX + 1, maxY - minY + 1);
    }

    public static int CountUniqueStructuresInRect(Vector2I start, Vector2I end, Func<Vector2I, FactoryStructure?> resolveStructure)
    {
        return CountUniqueStructuresInRect(BuildInclusiveRect(start, end), resolveStructure);
    }

    public static int CountUniqueStructuresInRect(Rect2I rect, Func<Vector2I, FactoryStructure?> resolveStructure)
    {
        var seen = new HashSet<ulong>();
        ForEachUniqueStructure(rect, resolveStructure, structure => seen.Add(structure.GetInstanceId()));
        return seen.Count;
    }

    public static List<Vector2I> CollectUniqueStructureAnchorCells(Vector2I start, Vector2I end, Func<Vector2I, FactoryStructure?> resolveStructure)
    {
        return CollectUniqueStructureAnchorCells(BuildInclusiveRect(start, end), resolveStructure);
    }

    public static List<Vector2I> CollectUniqueStructureAnchorCells(Rect2I rect, Func<Vector2I, FactoryStructure?> resolveStructure)
    {
        var cells = new List<Vector2I>();
        var seen = new HashSet<ulong>();
        ForEachUniqueStructure(
            rect,
            resolveStructure,
            structure =>
            {
                if (seen.Add(structure.GetInstanceId()))
                {
                    cells.Add(structure.Cell);
                }
            });
        return cells;
    }

    private static void ForEachUniqueStructure(Rect2I rect, Func<Vector2I, FactoryStructure?> resolveStructure, Action<FactoryStructure> visitor)
    {
        var seen = new HashSet<ulong>();
        for (var y = rect.Position.Y; y < rect.End.Y; y++)
        {
            for (var x = rect.Position.X; x < rect.End.X; x++)
            {
                var structure = resolveStructure(new Vector2I(x, y));
                if (structure is null || !seen.Add(structure.GetInstanceId()))
                {
                    continue;
                }

                visitor(structure);
            }
        }
    }
}
