using Godot;
using System.Collections.Generic;

public readonly record struct FactoryPortPreviewMarker(Vector2I Cell, bool IsInput, bool IsHighlighted);

public static class FactoryLogisticsPreview
{
    private static readonly Vector2I[] CandidateOffsets =
    {
        Vector2I.Zero,
        Vector2I.Left,
        Vector2I.Right,
        Vector2I.Up,
        Vector2I.Down,
        new Vector2I(-1, -1),
        new Vector2I(1, -1),
        new Vector2I(-1, 1),
        new Vector2I(1, 1)
    };

    public static bool ShouldShowContextualPortHints(BuildPrototypeKind kind)
    {
        return kind == BuildPrototypeKind.Belt;
    }

    public static List<FactoryPortPreviewMarker> CollectNearbyPortMarkers(IFactorySite site, Vector2I referenceCell)
    {
        var markers = new List<FactoryPortPreviewMarker>();
        var seenStructures = new HashSet<ulong>();
        var seenCells = new HashSet<(Vector2I, bool)>();

        for (var index = 0; index < CandidateOffsets.Length; index++)
        {
            if (!site.TryGetStructure(referenceCell + CandidateOffsets[index], out var structure) || structure is null)
            {
                continue;
            }

            if (!seenStructures.Add(structure.GetInstanceId()))
            {
                continue;
            }

            var inputCells = structure.GetInputCells();
            var outputCells = structure.GetOutputCells();
            if (inputCells.Count <= 1 && outputCells.Count <= 1)
            {
                continue;
            }

            AppendMarkers(markers, seenCells, inputCells, isInput: true, referenceCell);
            AppendMarkers(markers, seenCells, outputCells, isInput: false, referenceCell);
        }

        return markers;
    }

    private static void AppendMarkers(
        List<FactoryPortPreviewMarker> markers,
        HashSet<(Vector2I, bool)> seenCells,
        IReadOnlyList<Vector2I> cells,
        bool isInput,
        Vector2I referenceCell)
    {
        for (var index = 0; index < cells.Count; index++)
        {
            var markerKey = (cells[index], isInput);
            if (!seenCells.Add(markerKey))
            {
                continue;
            }

            markers.Add(new FactoryPortPreviewMarker(
                cells[index],
                isInput,
                cells[index] == referenceCell));
        }
    }
}
