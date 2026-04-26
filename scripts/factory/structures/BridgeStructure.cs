using Godot;
using NetFactory.Models;

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

    public override void RefreshPlacement()
    {
        Position = Site.CellToWorld(Cell);
        Rotation = new Vector3(0.0f, Site.WorldRotationRadians, 0.0f);
        Visible = Site.IsVisible;
    }

    protected override void BuildVisuals()
    {
        var builder = new DefaultModelBuilder(this, CellSize);
        BridgeModelDescriptor.BuildModel(builder, SiteKind);
    }

    protected override bool TryResolveTargetCell(FactoryItem item, Vector2I sourceCell, SimulationController simulation, out Vector2I targetCell)
    {
        targetCell = Cell + (Cell - sourceCell);
        return FactoryCargoRules.StructureAcceptsItem(Kind, FactoryIndustrialStandards.ResolveSiteKind(Site), item);
    }

    protected override int GetTransitLaneKey(Vector2I sourceCell, Vector2I targetCell)
    {
        var inputDelta = sourceCell - Cell;
        return Mathf.Abs(inputDelta.X) > 0 ? 0 : 1;
    }

    protected override Vector3 EvaluatePathPoint(TransitItemState state, float progress)
    {
        var edgeDistance = CellSize * 0.5f;
        var input = ToDirectionVector(state.SourceCell - Cell) * edgeDistance;
        var output = ToDirectionVector(state.TargetCell - Cell) * edgeDistance;
        var laneHeight = GetTransitLaneKey(state.SourceCell, state.TargetCell) == 0
            ? ItemHeight + 0.06f
            : ItemHeight - 0.08f;
        var point = input.Lerp(output, progress);
        return new Vector3(point.X, laneHeight, point.Y);
    }
}
