using Godot;
using NetFactory.Models;

public partial class UnloaderStructure : FlowTransportStructure
{
    protected override float TravelSpeed => FactoryConstants.BeltItemsPerSecond * 1.35f;

    public override BuildPrototypeKind Kind => BuildPrototypeKind.Unloader;
    public override string Description => "将机器端的输出卸到前方传送网络。";

    public override bool CanReceiveFrom(Vector2I sourceCell)
    {
        return sourceCell == GetInputCell();
    }

    public override bool CanOutputTo(Vector2I targetCell)
    {
        return targetCell == GetOutputCell();
    }

    protected override void BuildVisuals()
    {
        var builder = new DefaultModelBuilder(this, CellSize);
        UnloaderModelDescriptor.BuildModel(builder, SiteKind);
    }

    protected override bool TryResolveTargetCell(FactoryItem item, Vector2I sourceCell, SimulationController simulation, out Vector2I targetCell)
    {
        targetCell = GetOutputCell();

        if (!Site.TryGetStructure(sourceCell, out var source) || source is null || source is not IFactoryItemProvider || source.IsTransportNode)
        {
            return false;
        }

        if (Site.TryGetStructure(targetCell, out var target) && target is not null)
        {
            return target is IFactoryItemReceiver && target.IsTransportNode;
        }

        return true;
    }

    protected override Vector3 EvaluatePathPoint(TransitItemState state, float progress)
    {
        var start = new Vector3(-CellSize * 0.34f, ItemHeight, 0.0f);
        var end = new Vector3(CellSize * 0.38f, ItemHeight + 0.03f, 0.0f);
        return start.Lerp(end, progress);
    }
}
