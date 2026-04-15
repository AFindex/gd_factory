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

        var occupiedCells = ResolveOccupiedCells(previewKind, referenceCell, facing);
        AppendMarkers(
            markers,
            seenCells,
            GetPreviewInputCells(previewKind, referenceCell, facing),
            occupiedCells,
            isInput: true,
            highlightAll: true,
            referenceCell);
        AppendMarkers(
            markers,
            seenCells,
            GetPreviewOutputCells(previewKind, referenceCell, facing),
            occupiedCells,
            isInput: false,
            highlightAll: true,
            referenceCell);
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

            var inputCells = GetContextualInputCells(structure);
            var outputCells = GetContextualOutputCells(structure);
            if (inputCells.Count <= 0 && outputCells.Count <= 0)
            {
                continue;
            }

            var occupiedCells = ResolveOccupiedCells(structure.Kind, structure.Cell, structure.Facing);
            AppendMarkers(markers, seenCells, inputCells, occupiedCells, isInput: true, highlightAll: false, referenceCell);
            AppendMarkers(markers, seenCells, outputCells, occupiedCells, isInput: false, highlightAll: false, referenceCell);
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
        IReadOnlyList<Vector2I> cells,
        IReadOnlyList<Vector2I> occupiedCells,
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
                ResolvePortFacing(cells[index], occupiedCells, isInput),
                isInput,
                highlightAll || cells[index] == referenceCell));
        }
    }

    private static bool ShouldShowPreviewPortHints(BuildPrototypeKind kind)
    {
        if (kind is BuildPrototypeKind.CargoUnpacker or BuildPrototypeKind.CargoPacker)
        {
            return true;
        }

        var inputCount = GetPreviewInputCells(kind, Vector2I.Zero, FacingDirection.East).Count;
        var outputCount = GetPreviewOutputCells(kind, Vector2I.Zero, FacingDirection.East).Count;
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

    private static List<Vector2I> ResolveOccupiedCells(BuildPrototypeKind kind, Vector2I cell, FacingDirection facing)
    {
        var occupiedCells = new List<Vector2I>();
        foreach (var occupiedCell in FactoryStructureFactory.GetFootprint(kind).ResolveOccupiedCells(cell, facing))
        {
            occupiedCells.Add(occupiedCell);
        }

        return occupiedCells;
    }

    private static IReadOnlyList<Vector2I> GetPreviewInputCells(BuildPrototypeKind kind, Vector2I cell, FacingDirection facing)
    {
        if (kind is BuildPrototypeKind.CargoUnpacker or BuildPrototypeKind.CargoPacker)
        {
            return new[] { FactoryStructureFactory.GetFootprint(kind).ResolveInputCell(cell, facing) };
        }

        return kind == BuildPrototypeKind.Belt
            ? System.Array.Empty<Vector2I>()
            : FactoryTransportTopology.GetInputCells(kind, cell, facing);
    }

    private static IReadOnlyList<Vector2I> GetPreviewOutputCells(BuildPrototypeKind kind, Vector2I cell, FacingDirection facing)
    {
        if (kind is BuildPrototypeKind.CargoUnpacker or BuildPrototypeKind.CargoPacker)
        {
            return new[] { FactoryStructureFactory.GetFootprint(kind).ResolveOutputCell(cell, facing) };
        }

        return FactoryTransportTopology.GetOutputCells(kind, cell, facing);
    }

    private static IReadOnlyList<Vector2I> GetContextualInputCells(FactoryStructure structure)
    {
        if (structure.Kind is BuildPrototypeKind.CargoUnpacker or BuildPrototypeKind.CargoPacker)
        {
            return new[] { structure.GetInputCell() };
        }

        return structure.Kind == BuildPrototypeKind.Belt
            ? System.Array.Empty<Vector2I>()
            : FactoryTransportTopology.GetInputCells(structure);
    }

    private static IReadOnlyList<Vector2I> GetContextualOutputCells(FactoryStructure structure)
    {
        if (structure.Kind is BuildPrototypeKind.CargoUnpacker or BuildPrototypeKind.CargoPacker)
        {
            return new[] { structure.GetOutputCell() };
        }

        return FactoryTransportTopology.GetOutputCells(structure);
    }

    private static FacingDirection ResolvePortFacing(Vector2I portCell, IReadOnlyList<Vector2I> occupiedCells, bool isInput)
    {
        for (var index = 0; index < occupiedCells.Count; index++)
        {
            var occupiedCell = occupiedCells[index];
            if (IsOrthogonallyAdjacent(portCell, occupiedCell))
            {
                return ResolveFlowFacing(portCell, occupiedCell, isInput);
            }
        }

        return occupiedCells.Count > 0
            ? ResolveFlowFacing(portCell, occupiedCells[0], isInput)
            : (isInput ? FacingDirection.West : FacingDirection.East);
    }

    private static FacingDirection ResolveFlowFacing(Vector2I portCell, Vector2I occupiedCell, bool isInput)
    {
        var delta = occupiedCell - portCell;
        if (!isInput)
        {
            delta = -delta;
        }

        if (Mathf.Abs(delta.X) >= Mathf.Abs(delta.Y))
        {
            return delta.X >= 0 ? FacingDirection.East : FacingDirection.West;
        }

        return delta.Y >= 0 ? FacingDirection.South : FacingDirection.North;
    }

    private static bool IsOrthogonallyAdjacent(Vector2I a, Vector2I b)
    {
        var delta = a - b;
        return Mathf.Abs(delta.X) + Mathf.Abs(delta.Y) == 1;
    }
}
