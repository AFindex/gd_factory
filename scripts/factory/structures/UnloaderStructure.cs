using Godot;

public partial class UnloaderStructure : FlowTransportStructure
{
    protected override float TravelSpeed => FactoryConstants.BeltItemsPerSecond * 1.35f;

    public override BuildPrototypeKind Kind => BuildPrototypeKind.Unloader;
    public override string Description => "将后方传送网络中的物品卸入前方机器或回收端。";

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
        CreateColoredBox("Base", new Vector3(CellSize * 0.88f, 0.18f, CellSize * 0.88f), new Color("EA580C"), new Vector3(0.0f, 0.09f, 0.0f));
        CreateColoredBox("Ramp", new Vector3(CellSize * 0.70f, 0.12f, CellSize * 0.30f), new Color("FDBA74"), new Vector3(0.0f, 0.24f, 0.0f));
        CreateColoredBox("Arrow", new Vector3(CellSize * 0.26f, 0.05f, CellSize * 0.14f), new Color("FFEDD5"), new Vector3(CellSize * 0.24f, 0.34f, 0.0f));
        Rotation = new Vector3(0.0f, FactoryDirection.ToYRotationRadians(Facing), 0.0f);
    }

    protected override bool TryResolveTargetCell(FactoryItem item, Vector2I sourceCell, SimulationController simulation, out Vector2I targetCell)
    {
        targetCell = GetOutputCell();

        if (simulation.Grid is null || !simulation.Grid.TryGetStructure(sourceCell, out var source) || source is null || !source.IsTransportNode)
        {
            return false;
        }

        if (simulation.Grid.TryGetStructure(targetCell, out var target) && target is not null)
        {
            return !target.IsTransportNode;
        }

        return true;
    }
}
