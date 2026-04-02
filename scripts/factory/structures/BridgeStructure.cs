using Godot;

public partial class BridgeStructure : FlowTransportStructure
{
    protected override float TravelSpeed => FactoryConstants.BeltItemsPerSecond * 1.15f;
    protected override float ItemHeight => 0.52f;

    public override BuildPrototypeKind Kind => BuildPrototypeKind.Bridge;
    public override string Description => "让南北和东西两路物流在同一格互相跨越，不发生连接。";

    public override bool CanReceiveFrom(Vector2I sourceCell)
    {
        var delta = Cell - sourceCell;
        return Mathf.Abs(delta.X) + Mathf.Abs(delta.Y) == 1;
    }

    public override bool CanOutputTo(Vector2I targetCell)
    {
        var delta = targetCell - Cell;
        return Mathf.Abs(delta.X) + Mathf.Abs(delta.Y) == 1;
    }

    protected override void BuildVisuals()
    {
        CreateColoredBox("Base", new Vector3(CellSize * 0.92f, 0.16f, CellSize * 0.92f), new Color("475569"), new Vector3(0.0f, 0.08f, 0.0f));
        CreateColoredBox("EastWest", new Vector3(CellSize * 0.95f, 0.10f, CellSize * 0.20f), new Color("F59E0B"), new Vector3(0.0f, 0.38f, 0.0f));
        CreateColoredBox("NorthSouth", new Vector3(CellSize * 0.20f, 0.10f, CellSize * 0.95f), new Color("38BDF8"), new Vector3(0.0f, 0.22f, 0.0f));
    }

    protected override bool TryResolveTargetCell(FactoryItem item, Vector2I sourceCell, SimulationController simulation, out Vector2I targetCell)
    {
        targetCell = Cell + (Cell - sourceCell);
        return true;
    }

    protected override int GetTransitLaneKey(Vector2I sourceCell, Vector2I targetCell)
    {
        var inputDelta = sourceCell - Cell;
        return Mathf.Abs(inputDelta.X) > 0 ? 0 : 1;
    }
}
