using Godot;
using NetFactory.Models;

public partial class MergerStructure : FlowTransportStructure
{
    public override BuildPrototypeKind Kind => BuildPrototypeKind.Merger;
    public override string Description => "将后方、左侧和右侧三路物流汇入前方单一路径。";

    public override bool CanReceiveFrom(Vector2I sourceCell)
    {
        return FactoryTransportTopology.MergerCanReceiveFrom(Cell, Facing, sourceCell);
    }

    public override bool CanOutputTo(Vector2I targetCell)
    {
        return targetCell == GetOutputCell();
    }

    protected override void BuildVisuals()
    {
        var builder = new DefaultModelBuilder(this, CellSize);
        MergerModelDescriptor.BuildModel(builder, SiteKind);
    }

    protected override bool TryResolveTargetCell(FactoryItem item, Vector2I sourceCell, SimulationController simulation, out Vector2I targetCell)
    {
        targetCell = GetOutputCell();
        return FactoryCargoRules.StructureAcceptsItem(Kind, FactoryIndustrialStandards.ResolveSiteKind(Site), item);
    }

    protected override float GetTransitVisualYawCompensation(TransitItemState state)
    {
        return -FactoryDirection.ToYRotationRadians(Facing);
    }

    protected override Vector3 EvaluatePathPoint(TransitItemState state, float progress)
    {
        var edgeDistance = CellSize * 0.5f;
        var input = ToDirectionVector(state.SourceCell - Cell).Rotated(FactoryDirection.ToYRotationRadians(Facing)) * edgeDistance;
        var output = ToDirectionVector(state.TargetCell - Cell).Rotated(FactoryDirection.ToYRotationRadians(Facing)) * edgeDistance;
        var midpointRatio = 0.42f;
        if (progress <= midpointRatio)
        {
            var entryProgress = progress / midpointRatio;
            var point = input.Lerp(Vector2.Zero, entryProgress);
            return new Vector3(point.X, ItemHeight, point.Y);
        }

        var exitProgress = (progress - midpointRatio) / (1.0f - midpointRatio);
        var exitPoint = Vector2.Zero.Lerp(output, exitProgress);
        return new Vector3(exitPoint.X, ItemHeight, exitPoint.Y);
    }
}
