using Godot;

public partial class SplitterStructure : FlowTransportStructure
{
    private bool _sendLeftNext = true;

    public override BuildPrototypeKind Kind => BuildPrototypeKind.Splitter;
    public override string Description => "将后方输入的物流分到左右两路输出。";

    public override bool CanReceiveFrom(Vector2I sourceCell)
    {
        return sourceCell == Cell - FactoryDirection.ToCellOffset(Facing);
    }

    public override bool CanOutputTo(Vector2I targetCell)
    {
        return targetCell == GetLeftOutputCell() || targetCell == GetRightOutputCell();
    }

    protected override void BuildVisuals()
    {
        CreateColoredBox("Body", new Vector3(CellSize * 0.86f, 0.24f, CellSize * 0.86f), new Color("8B5CF6"), new Vector3(0.0f, 0.12f, 0.0f));
        CreateColoredBox("InputStem", new Vector3(CellSize * 0.42f, 0.10f, CellSize * 0.18f), new Color("C4B5FD"), new Vector3(-CellSize * 0.28f, 0.2f, 0.0f));
        CreateColoredBox("TopStem", new Vector3(CellSize * 0.22f, 0.10f, CellSize * 0.34f), new Color("DDD6FE"), new Vector3(CellSize * 0.18f, 0.2f, -CellSize * 0.18f));
        CreateColoredBox("BottomStem", new Vector3(CellSize * 0.22f, 0.10f, CellSize * 0.34f), new Color("DDD6FE"), new Vector3(CellSize * 0.18f, 0.2f, CellSize * 0.18f));
    }

    protected override bool TryResolveTargetCell(FactoryItem item, Vector2I sourceCell, SimulationController simulation, out Vector2I targetCell)
    {
        var leftCell = GetLeftOutputCell();
        var rightCell = GetRightOutputCell();
        var preferLeft = _sendLeftNext;

        if (CanConnectTo(leftCell) && CanConnectTo(rightCell))
        {
            targetCell = preferLeft ? leftCell : rightCell;
            return true;
        }

        if (CanConnectTo(leftCell))
        {
            targetCell = leftCell;
            return true;
        }

        if (CanConnectTo(rightCell))
        {
            targetCell = rightCell;
            return true;
        }

        targetCell = preferLeft ? leftCell : rightCell;
        return true;
    }

    protected override void OnTransitItemAccepted(TransitItemState state)
    {
        _sendLeftNext = !_sendLeftNext;
    }

    protected override bool TryDispatchItem(TransitItemState state, SimulationController simulation)
    {
        var leftCell = GetLeftOutputCell();
        var rightCell = GetRightOutputCell();
        var primaryCell = state.TargetCell == rightCell ? rightCell : leftCell;
        var secondaryCell = primaryCell == leftCell ? rightCell : leftCell;
        var primaryAvailable = CanRouteToNow(primaryCell, state.Item, simulation);
        var secondaryAvailable = CanRouteToNow(secondaryCell, state.Item, simulation);

        if (primaryAvailable)
        {
            state.TargetCell = primaryCell;
            if (simulation.TrySendItem(this, primaryCell, state.Item))
            {
                return true;
            }
        }

        if (secondaryAvailable)
        {
            state.TargetCell = secondaryCell;
            if (simulation.TrySendItem(this, secondaryCell, state.Item))
            {
                return true;
            }
        }

        if (!primaryAvailable && secondaryAvailable)
        {
            state.TargetCell = secondaryCell;
        }
        else if (!secondaryAvailable)
        {
            state.TargetCell = primaryCell;
        }

        return false;
    }

    protected override void RefreshTransitTargets(SimulationController simulation)
    {
        for (var index = 0; index < TransitItems.Count; index++)
        {
            var state = TransitItems[index];
            if (TryResolveDynamicTarget(state.TargetCell, state.Item, simulation, out var targetCell))
            {
                state.TargetCell = targetCell;
            }
        }
    }

    protected override Vector3 EvaluatePathPoint(TransitItemState state, float progress)
    {
        var edgeDistance = CellSize * 0.5f;
        var input = ToDirectionVector(state.SourceCell - Cell).Rotated(FactoryDirection.ToYRotationRadians(Facing)) * edgeDistance;
        var output = ToDirectionVector(state.TargetCell - Cell).Rotated(FactoryDirection.ToYRotationRadians(Facing)) * edgeDistance;
        var oneMinus = 1.0f - progress;
        var point2D =
            oneMinus * oneMinus * input +
            2.0f * oneMinus * progress * Vector2.Zero +
            progress * progress * output;

        return new Vector3(point2D.X, ItemHeight, point2D.Y);
    }

    private Vector2I GetLeftOutputCell()
    {
        return Cell + FactoryDirection.ToCellOffset(FactoryDirection.RotateCounterClockwise(Facing));
    }

    private Vector2I GetRightOutputCell()
    {
        return Cell + FactoryDirection.ToCellOffset(FactoryDirection.RotateClockwise(Facing));
    }

    private bool TryResolveDynamicTarget(Vector2I currentTargetCell, FactoryItem item, SimulationController simulation, out Vector2I targetCell)
    {
        var leftCell = GetLeftOutputCell();
        var rightCell = GetRightOutputCell();
        var preferredCell = currentTargetCell == rightCell ? rightCell : leftCell;
        var alternateCell = preferredCell == leftCell ? rightCell : leftCell;
        var preferredAvailable = CanRouteToNow(preferredCell, item, simulation);
        var alternateAvailable = CanRouteToNow(alternateCell, item, simulation);

        if (preferredAvailable || !alternateAvailable)
        {
            targetCell = preferredCell;
            return true;
        }

        targetCell = alternateCell;
        return true;
    }

    private bool CanConnectTo(Vector2I cell)
    {
        return Site.TryGetStructure(cell, out var structure)
            && structure is not null
            && structure.CanReceiveFrom(Cell);
    }

    private bool CanRouteToNow(Vector2I cell, FactoryItem item, SimulationController simulation)
    {
        return Site.TryGetStructure(cell, out var structure)
            && structure is not null
            && structure.CanAcceptItem(item, Cell, simulation);
    }
}
