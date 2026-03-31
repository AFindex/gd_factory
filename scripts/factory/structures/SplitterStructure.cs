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
        Rotation = new Vector3(0.0f, FactoryDirection.ToYRotationRadians(Facing), 0.0f);
    }

    protected override bool TryResolveTargetCell(FactoryItem item, Vector2I sourceCell, SimulationController simulation, out Vector2I targetCell)
    {
        var leftCell = GetLeftOutputCell();
        var rightCell = GetRightOutputCell();
        var preferLeft = _sendLeftNext;
        _sendLeftNext = !_sendLeftNext;

        if (CanConnectTo(leftCell, simulation) && CanConnectTo(rightCell, simulation))
        {
            targetCell = preferLeft ? leftCell : rightCell;
            return true;
        }

        if (CanConnectTo(leftCell, simulation))
        {
            targetCell = leftCell;
            return true;
        }

        if (CanConnectTo(rightCell, simulation))
        {
            targetCell = rightCell;
            return true;
        }

        targetCell = preferLeft ? leftCell : rightCell;
        return true;
    }

    private Vector2I GetLeftOutputCell()
    {
        return Cell + FactoryDirection.ToCellOffset(FactoryDirection.RotateCounterClockwise(Facing));
    }

    private Vector2I GetRightOutputCell()
    {
        return Cell + FactoryDirection.ToCellOffset(FactoryDirection.RotateClockwise(Facing));
    }

    private bool CanConnectTo(Vector2I cell, SimulationController simulation)
    {
        return Site.TryGetStructure(cell, out var structure)
            && structure is not null
            && structure.CanReceiveFrom(Cell);
    }
}
