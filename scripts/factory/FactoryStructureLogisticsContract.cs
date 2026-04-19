using Godot;
using System;
using System.Collections.Generic;

public readonly record struct FactoryStructureLogisticsAnchor(
    Vector2I Cell,
    FacingDirection Facing,
    bool IsInput,
    Vector2I DispatchSourceCell,
    bool IsIntegratedCell);

public readonly record struct FactoryItemHandoffDescriptor(
    FactoryStructure Provider,
    FactoryStructure Receiver,
    Vector2I TargetCell,
    Vector2I ProviderDispatchCell,
    Vector2I ReceiverAcceptanceCell,
    bool ReceiverResolvedFromContractEdge);

public sealed class FactoryStructureLogisticsContractDefinition
{
    private readonly Func<Vector2I, FacingDirection, IReadOnlyList<Vector2I>> _resolveInputCells;
    private readonly Func<Vector2I, FacingDirection, IReadOnlyList<Vector2I>> _resolveOutputCells;
    private readonly Func<Vector2I, FacingDirection, Vector2I, Vector2I> _resolveDispatchSourceCell;

    public FactoryStructureLogisticsContractDefinition(
        FactoryStructureFootprint footprint,
        Func<Vector2I, FacingDirection, IReadOnlyList<Vector2I>>? resolveInputCells = null,
        Func<Vector2I, FacingDirection, IReadOnlyList<Vector2I>>? resolveOutputCells = null,
        Func<Vector2I, FacingDirection, Vector2I, Vector2I>? resolveDispatchSourceCell = null)
    {
        Footprint = footprint;
        _resolveInputCells = resolveInputCells ?? footprint.ResolveInputCells;
        _resolveOutputCells = resolveOutputCells ?? footprint.ResolveOutputCells;
        _resolveDispatchSourceCell = resolveDispatchSourceCell ?? footprint.ResolveOutputTransferCell;
    }

    public FactoryStructureFootprint Footprint { get; }

    public IReadOnlyList<Vector2I> ResolveOccupiedCells(Vector2I anchorCell, FacingDirection facing)
    {
        var cells = new List<Vector2I>();
        foreach (var occupiedCell in Footprint.ResolveOccupiedCells(anchorCell, facing))
        {
            cells.Add(occupiedCell);
        }

        return cells;
    }

    public IReadOnlyList<Vector2I> ResolveInputCells(Vector2I anchorCell, FacingDirection facing)
    {
        return _resolveInputCells(anchorCell, facing);
    }

    public IReadOnlyList<Vector2I> ResolveOutputCells(Vector2I anchorCell, FacingDirection facing)
    {
        return _resolveOutputCells(anchorCell, facing);
    }

    public Vector2I ResolveDispatchSourceCell(Vector2I anchorCell, FacingDirection facing, Vector2I targetCell)
    {
        return _resolveDispatchSourceCell(anchorCell, facing, targetCell);
    }

    public static FactoryStructureLogisticsContractDefinition FromFootprint(FactoryStructureFootprint footprint)
    {
        return new FactoryStructureLogisticsContractDefinition(footprint);
    }
}

public readonly struct ResolvedFactoryStructureLogisticsContract
{
    public ResolvedFactoryStructureLogisticsContract(
        BuildPrototypeKind kind,
        Vector2I anchorCell,
        FacingDirection facing,
        FactoryStructureLogisticsContractDefinition definition,
        IReadOnlyList<Vector2I> occupiedCells,
        IReadOnlyList<Vector2I> inputCells,
        IReadOnlyList<Vector2I> outputCells,
        IReadOnlyList<FactoryStructureLogisticsAnchor> inputAnchors,
        IReadOnlyList<FactoryStructureLogisticsAnchor> outputAnchors)
    {
        Kind = kind;
        AnchorCell = anchorCell;
        Facing = facing;
        Definition = definition;
        OccupiedCells = occupiedCells;
        InputCells = inputCells;
        OutputCells = outputCells;
        InputAnchors = inputAnchors;
        OutputAnchors = outputAnchors;
    }

    public BuildPrototypeKind Kind { get; }
    public Vector2I AnchorCell { get; }
    public FacingDirection Facing { get; }
    public FactoryStructureLogisticsContractDefinition Definition { get; }
    public FactoryStructureFootprint Footprint => Definition.Footprint;
    public IReadOnlyList<Vector2I> OccupiedCells { get; }
    public IReadOnlyList<Vector2I> InputCells { get; }
    public IReadOnlyList<Vector2I> OutputCells { get; }
    public IReadOnlyList<FactoryStructureLogisticsAnchor> InputAnchors { get; }
    public IReadOnlyList<FactoryStructureLogisticsAnchor> OutputAnchors { get; }

    public bool ContainsOccupiedCell(Vector2I cell)
    {
        return FactoryStructureLogisticsContractResolver.ContainsCell(OccupiedCells, cell);
    }

    public bool TryGetInputAnchor(Vector2I cell, out FactoryStructureLogisticsAnchor anchor)
    {
        return FactoryStructureLogisticsContractResolver.TryFindAnchor(InputAnchors, cell, out anchor);
    }

    public bool TryGetOutputAnchor(Vector2I cell, out FactoryStructureLogisticsAnchor anchor)
    {
        return FactoryStructureLogisticsContractResolver.TryFindAnchor(OutputAnchors, cell, out anchor);
    }

    public Vector2I ResolveDispatchSourceCell(Vector2I targetCell, Vector2I fallbackSourceCell)
    {
        return TryGetOutputAnchor(targetCell, out var anchor)
            ? anchor.DispatchSourceCell
            : fallbackSourceCell;
    }

    public Vector3 GetWorldCenterOffset(float cellSize)
    {
        return Footprint.GetWorldCenterOffset(cellSize, Facing);
    }

    public Vector2 GetPreviewSize(float cellSize)
    {
        return Footprint.GetPreviewSize(cellSize, Facing);
    }
}

public static class FactoryStructureLogisticsContractResolver
{
    public static FactoryStructureLogisticsContractDefinition GetDefinition(
        BuildPrototypeKind kind,
        IReadOnlyDictionary<string, string>? configuration = null,
        string? mapRecipeId = null)
    {
        return FactoryStructureFactory.GetLogisticsContractDefinition(kind, configuration, mapRecipeId);
    }

    public static ResolvedFactoryStructureLogisticsContract Resolve(
        BuildPrototypeKind kind,
        Vector2I anchorCell,
        FacingDirection facing,
        IReadOnlyDictionary<string, string>? configuration = null,
        string? mapRecipeId = null)
    {
        var definition = GetDefinition(kind, configuration, mapRecipeId);
        return Resolve(kind, anchorCell, facing, definition);
    }

    public static ResolvedFactoryStructureLogisticsContract Resolve(FactoryStructure structure)
    {
        var definition = FactoryStructureFactory.GetLogisticsContractDefinition(
            structure.Kind,
            configuration: null,
            mapRecipeId: structure.CaptureMapRecipeId(),
            footprintOverride: structure.ResolvedFootprint);
        return Resolve(structure.Kind, structure.Cell, structure.Facing, definition);
    }

    public static ResolvedFactoryStructureLogisticsContract Resolve(
        BuildPrototypeKind kind,
        Vector2I anchorCell,
        FacingDirection facing,
        FactoryStructureLogisticsContractDefinition definition)
    {
        var occupiedCells = definition.ResolveOccupiedCells(anchorCell, facing);
        var inputCells = definition.ResolveInputCells(anchorCell, facing);
        var outputCells = definition.ResolveOutputCells(anchorCell, facing);

        var inputAnchors = BuildAnchors(
            inputCells,
            occupiedCells,
            isInput: true,
            dispatchSourceCell: cell => cell);
        var outputAnchors = BuildAnchors(
            outputCells,
            occupiedCells,
            isInput: false,
            dispatchSourceCell: cell => definition.ResolveDispatchSourceCell(anchorCell, facing, cell));

        return new ResolvedFactoryStructureLogisticsContract(
            kind,
            anchorCell,
            facing,
            definition,
            occupiedCells,
            inputCells,
            outputCells,
            inputAnchors,
            outputAnchors);
    }

    public static bool TryFindAnchor(
        IReadOnlyList<FactoryStructureLogisticsAnchor> anchors,
        Vector2I cell,
        out FactoryStructureLogisticsAnchor anchor)
    {
        for (var index = 0; index < anchors.Count; index++)
        {
            if (anchors[index].Cell == cell)
            {
                anchor = anchors[index];
                return true;
            }
        }

        anchor = default;
        return false;
    }

    public static bool ContainsCell(IReadOnlyList<Vector2I> cells, Vector2I targetCell)
    {
        for (var index = 0; index < cells.Count; index++)
        {
            if (cells[index] == targetCell)
            {
                return true;
            }
        }

        return false;
    }

    internal static FacingDirection ResolveAnchorFacing(Vector2I portCell, IReadOnlyList<Vector2I> occupiedCells, bool isInput)
    {
        if (ContainsCell(occupiedCells, portCell))
        {
            return ResolveFacingFromIntegratedCell(portCell, occupiedCells, isInput);
        }

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

    private static IReadOnlyList<FactoryStructureLogisticsAnchor> BuildAnchors(
        IReadOnlyList<Vector2I> cells,
        IReadOnlyList<Vector2I> occupiedCells,
        bool isInput,
        Func<Vector2I, Vector2I> dispatchSourceCell)
    {
        var anchors = new FactoryStructureLogisticsAnchor[cells.Count];
        for (var index = 0; index < cells.Count; index++)
        {
            anchors[index] = new FactoryStructureLogisticsAnchor(
                cells[index],
                ResolveAnchorFacing(cells[index], occupiedCells, isInput),
                isInput,
                dispatchSourceCell(cells[index]),
                ContainsCell(occupiedCells, cells[index]));
        }

        return anchors;
    }

    private static FacingDirection ResolveFacingFromIntegratedCell(
        Vector2I portCell,
        IReadOnlyList<Vector2I> occupiedCells,
        bool isInput)
    {
        var center = Vector2.Zero;
        for (var index = 0; index < occupiedCells.Count; index++)
        {
            center += new Vector2(occupiedCells[index].X, occupiedCells[index].Y);
        }

        center /= occupiedCells.Count;
        var delta = new Vector2(portCell.X, portCell.Y) - center;
        if (delta.LengthSquared() <= 0.0001f)
        {
            return isInput ? FacingDirection.West : FacingDirection.East;
        }

        if (Mathf.Abs(delta.X) >= Mathf.Abs(delta.Y))
        {
            return delta.X >= 0.0f ? FacingDirection.East : FacingDirection.West;
        }

        return delta.Y >= 0.0f ? FacingDirection.South : FacingDirection.North;
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
