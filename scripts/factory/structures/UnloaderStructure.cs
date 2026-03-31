using Godot;

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
        CreateColoredBox("Base", new Vector3(CellSize * 0.88f, 0.18f, CellSize * 0.88f), new Color("2563EB"), new Vector3(0.0f, 0.09f, 0.0f));
        CreateColoredBox("BackHousing", new Vector3(CellSize * 0.36f, 0.34f, CellSize * 0.60f), new Color("1D4ED8"), new Vector3(-CellSize * 0.22f, 0.26f, 0.0f));
        CreateColoredBox("FeedBed", new Vector3(CellSize * 0.56f, 0.10f, CellSize * 0.26f), new Color("93C5FD"), new Vector3(0.02f, 0.22f, 0.0f));
        CreateColoredBox("FrontNozzle", new Vector3(CellSize * 0.18f, 0.18f, CellSize * 0.22f), new Color("DBEAFE"), new Vector3(CellSize * 0.34f, 0.28f, 0.0f));
        CreateColoredBox("DirectionMark", new Vector3(CellSize * 0.18f, 0.05f, CellSize * 0.12f), new Color("EFF6FF"), new Vector3(CellSize * 0.22f, 0.40f, 0.0f));
        Rotation = new Vector3(0.0f, FactoryDirection.ToYRotationRadians(Facing), 0.0f);
    }

    protected override bool TryResolveTargetCell(FactoryItem item, Vector2I sourceCell, SimulationController simulation, out Vector2I targetCell)
    {
        targetCell = GetOutputCell();

        if (simulation.Grid is null || !simulation.Grid.TryGetStructure(sourceCell, out var source) || source is null || source.IsTransportNode)
        {
            return false;
        }

        if (simulation.Grid.TryGetStructure(targetCell, out var target) && target is not null)
        {
            return target.IsTransportNode;
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
