using Godot;

public partial class MergerStructure : FlowTransportStructure
{
    public override BuildPrototypeKind Kind => BuildPrototypeKind.Merger;
    public override string Description => "将左右两路物流汇入前方单一路径。";

    public override bool CanReceiveFrom(Vector2I sourceCell)
    {
        return sourceCell == GetLeftInputCell() || sourceCell == GetRightInputCell();
    }

    public override bool CanOutputTo(Vector2I targetCell)
    {
        return targetCell == GetOutputCell();
    }

    protected override void BuildVisuals()
    {
        CreateColoredBox("Body", new Vector3(CellSize * 0.86f, 0.24f, CellSize * 0.86f), new Color("14B8A6"), new Vector3(0.0f, 0.12f, 0.0f));
        CreateColoredBox("OutputStem", new Vector3(CellSize * 0.42f, 0.10f, CellSize * 0.18f), new Color("99F6E4"), new Vector3(CellSize * 0.28f, 0.2f, 0.0f));
        CreateColoredBox("TopStem", new Vector3(CellSize * 0.22f, 0.10f, CellSize * 0.34f), new Color("CCFBF1"), new Vector3(-CellSize * 0.18f, 0.2f, -CellSize * 0.18f));
        CreateColoredBox("BottomStem", new Vector3(CellSize * 0.22f, 0.10f, CellSize * 0.34f), new Color("CCFBF1"), new Vector3(-CellSize * 0.18f, 0.2f, CellSize * 0.18f));
    }

    protected override bool TryResolveTargetCell(FactoryItem item, Vector2I sourceCell, SimulationController simulation, out Vector2I targetCell)
    {
        targetCell = GetOutputCell();
        return true;
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

    private Vector2I GetLeftInputCell()
    {
        return Cell + FactoryDirection.ToCellOffset(FactoryDirection.RotateCounterClockwise(Facing));
    }

    private Vector2I GetRightInputCell()
    {
        return Cell + FactoryDirection.ToCellOffset(FactoryDirection.RotateClockwise(Facing));
    }
}
