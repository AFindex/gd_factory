using Godot;
using System.Collections.Generic;

public readonly record struct FactoryPortPreviewMarker(Vector2I Cell, FacingDirection Facing, bool IsInput, bool IsHighlighted);

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
        return ShouldShowPreviewPortHints(kind) || ShouldShowNearbyContextualPortHints(kind);
    }

    public static List<FactoryPortPreviewMarker> CollectPortMarkers(
        IFactorySite site,
        BuildPrototypeKind previewKind,
        Vector2I referenceCell,
        FacingDirection facing,
        IEnumerable<FactoryStructure>? visibleStructures = null)
    {
        var markers = new List<FactoryPortPreviewMarker>();
        var seenCells = new HashSet<(Vector2I, bool)>();

        AppendPreviewMarkers(markers, seenCells, previewKind, referenceCell, facing);
        if (ShouldShowNearbyContextualPortHints(previewKind))
        {
            if (visibleStructures is not null)
            {
                AppendVisibleStructurePortMarkers(markers, seenCells, visibleStructures, referenceCell);
            }

            AppendNearbyPortMarkers(markers, seenCells, site, referenceCell);
        }

        return markers;
    }

    private static void AppendPreviewMarkers(
        List<FactoryPortPreviewMarker> markers,
        HashSet<(Vector2I, bool)> seenCells,
        BuildPrototypeKind previewKind,
        Vector2I referenceCell,
        FacingDirection facing)
    {
        if (!ShouldShowPreviewPortHints(previewKind))
        {
            return;
        }

        var contract = FactoryStructureLogisticsContractResolver.Resolve(previewKind, referenceCell, facing);
        AppendMarkers(markers, seenCells, contract.InputAnchors, highlightAll: true, referenceCell);
        AppendMarkers(markers, seenCells, contract.OutputAnchors, highlightAll: true, referenceCell);
    }

    private static void AppendVisibleStructurePortMarkers(
        List<FactoryPortPreviewMarker> markers,
        HashSet<(Vector2I, bool)> seenCells,
        IEnumerable<FactoryStructure> visibleStructures,
        Vector2I referenceCell)
    {
        var seenStructures = new HashSet<ulong>();
        foreach (var structure in visibleStructures)
        {
            if (structure is null)
            {
                continue;
            }

            if (!seenStructures.Add(structure.GetInstanceId()))
            {
                continue;
            }

            var contract = structure.GetResolvedLogisticsContract();
            var inputAnchors = GetContextualInputAnchors(structure, contract);
            var outputAnchors = GetContextualOutputAnchors(structure, contract);
            if (inputAnchors.Count <= 0 && outputAnchors.Count <= 0)
            {
                continue;
            }

            AppendMarkers(markers, seenCells, inputAnchors, highlightAll: false, referenceCell);
            AppendMarkers(markers, seenCells, outputAnchors, highlightAll: false, referenceCell);
        }
    }

    private static void AppendNearbyPortMarkers(
        List<FactoryPortPreviewMarker> markers,
        HashSet<(Vector2I, bool)> seenCells,
        IFactorySite site,
        Vector2I referenceCell)
    {
        var nearbyStructures = new List<FactoryStructure>();
        var seenStructures = new HashSet<ulong>();
        for (var index = 0; index < CandidateOffsets.Length; index++)
        {
            if (!site.TryGetStructure(referenceCell + CandidateOffsets[index], out var structure) || structure is null)
            {
                continue;
            }

            if (seenStructures.Add(structure.GetInstanceId()))
            {
                nearbyStructures.Add(structure);
            }
        }

        AppendVisibleStructurePortMarkers(markers, seenCells, nearbyStructures, referenceCell);
    }

    private static void AppendMarkers(
        List<FactoryPortPreviewMarker> markers,
        HashSet<(Vector2I, bool)> seenCells,
        IReadOnlyList<FactoryStructureLogisticsAnchor> anchors,
        bool highlightAll,
        Vector2I referenceCell)
    {
        for (var index = 0; index < anchors.Count; index++)
        {
            var markerKey = (anchors[index].Cell, anchors[index].IsInput);
            if (!seenCells.Add(markerKey))
            {
                continue;
            }

            markers.Add(new FactoryPortPreviewMarker(
                anchors[index].Cell,
                anchors[index].Facing,
                anchors[index].IsInput,
                highlightAll || anchors[index].Cell == referenceCell));
        }
    }

    private static bool ShouldShowPreviewPortHints(BuildPrototypeKind kind)
    {
        if (kind == BuildPrototypeKind.Belt)
        {
            return false;
        }

        var contract = FactoryStructureLogisticsContractResolver.Resolve(kind, Vector2I.Zero, FacingDirection.East);
        var inputCount = contract.InputAnchors.Count;
        var outputCount = contract.OutputAnchors.Count;
        return inputCount > 1 || outputCount > 1;
    }

    private static bool ShouldShowNearbyContextualPortHints(BuildPrototypeKind kind)
    {
        return kind == BuildPrototypeKind.Belt
            || kind == BuildPrototypeKind.Splitter
            || kind == BuildPrototypeKind.Merger
            || kind == BuildPrototypeKind.Inserter
            || kind == BuildPrototypeKind.Loader
            || kind == BuildPrototypeKind.Unloader
            || kind == BuildPrototypeKind.Bridge;
    }

    private static IReadOnlyList<FactoryStructureLogisticsAnchor> GetContextualInputAnchors(
        FactoryStructure structure,
        ResolvedFactoryStructureLogisticsContract contract)
    {
        return structure.Kind == BuildPrototypeKind.Belt
            ? System.Array.Empty<FactoryStructureLogisticsAnchor>()
            : contract.InputAnchors;
    }

    private static IReadOnlyList<FactoryStructureLogisticsAnchor> GetContextualOutputAnchors(
        FactoryStructure structure,
        ResolvedFactoryStructureLogisticsContract contract)
    {
        return contract.OutputAnchors;
    }
}
