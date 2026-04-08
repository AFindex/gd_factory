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
        return kind == BuildPrototypeKind.Belt || kind == BuildPrototypeKind.Merger;
    }

    public static List<FactoryPortPreviewMarker> CollectPortMarkers(IFactorySite site, BuildPrototypeKind previewKind, Vector2I referenceCell, FacingDirection facing)
    {
        var markers = new List<FactoryPortPreviewMarker>();
        var seenCells = new HashSet<(Vector2I, bool)>();

        AppendPreviewMarkers(markers, seenCells, previewKind, referenceCell, facing);
        AppendNearbyPortMarkers(markers, seenCells, site, referenceCell);
        return markers;
    }

    private static void AppendPreviewMarkers(
        List<FactoryPortPreviewMarker> markers,
        HashSet<(Vector2I, bool)> seenCells,
        BuildPrototypeKind previewKind,
        Vector2I referenceCell,
        FacingDirection facing)
    {
        if (previewKind != BuildPrototypeKind.Merger)
        {
            return;
        }

        AppendMarkers(
            markers,
            seenCells,
            FactoryTransportTopology.GetInputCells(previewKind, referenceCell, facing),
            isInput: true,
            highlightAll: true,
            referenceCell);
        AppendMarkers(
            markers,
            seenCells,
            FactoryTransportTopology.GetOutputCells(previewKind, referenceCell, facing),
            isInput: false,
            highlightAll: true,
            referenceCell);
    }

    private static void AppendNearbyPortMarkers(
        List<FactoryPortPreviewMarker> markers,
        HashSet<(Vector2I, bool)> seenCells,
        IFactorySite site,
        Vector2I referenceCell)
    {
        var seenStructures = new HashSet<ulong>();
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

            var inputCells = FactoryTransportTopology.GetInputCells(structure);
            var outputCells = FactoryTransportTopology.GetOutputCells(structure);
            if (inputCells.Count <= 1 && outputCells.Count <= 1)
            {
                continue;
            }

            AppendMarkers(markers, seenCells, inputCells, isInput: true, highlightAll: false, referenceCell);
            AppendMarkers(markers, seenCells, outputCells, isInput: false, highlightAll: false, referenceCell);
        }
    }

    private static void AppendMarkers(
        List<FactoryPortPreviewMarker> markers,
        HashSet<(Vector2I, bool)> seenCells,
        IReadOnlyList<Vector2I> cells,
        bool isInput,
        bool highlightAll,
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
                highlightAll || cells[index] == referenceCell));
        }
    }
}
