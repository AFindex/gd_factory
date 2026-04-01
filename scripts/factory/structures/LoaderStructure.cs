using Godot;

public partial class LoaderStructure : FlowTransportStructure
{
    protected override float TravelSpeed => FactoryConstants.BeltItemsPerSecond * 1.35f;

    public override BuildPrototypeKind Kind => BuildPrototypeKind.Loader;
    public override string Description => "将后方传送网络中的物品装入前方机器或回收端。";

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
        CreateColoredBox("FrontHopper", new Vector3(CellSize * 0.36f, 0.34f, CellSize * 0.60f), new Color("C2410C"), new Vector3(CellSize * 0.22f, 0.26f, 0.0f));
        CreateColoredBox("FeedBed", new Vector3(CellSize * 0.56f, 0.10f, CellSize * 0.26f), new Color("FDBA74"), new Vector3(-0.02f, 0.22f, 0.0f));
        CreateColoredBox("RearMouth", new Vector3(CellSize * 0.18f, 0.18f, CellSize * 0.22f), new Color("FFEDD5"), new Vector3(-CellSize * 0.34f, 0.28f, 0.0f));
        CreateColoredBox("DirectionMark", new Vector3(CellSize * 0.18f, 0.05f, CellSize * 0.12f), new Color("FFF7ED"), new Vector3(-CellSize * 0.22f, 0.40f, 0.0f));
    }

    protected override bool TryResolveTargetCell(FactoryItem item, Vector2I sourceCell, SimulationController simulation, out Vector2I targetCell)
    {
        targetCell = GetOutputCell();

        if (!Site.TryGetStructure(sourceCell, out var source) || source is null || !source.IsTransportNode)
        {
            return false;
        }

        if (Site.TryGetStructure(targetCell, out var target) && target is not null)
        {
            return !target.IsTransportNode;
        }

        return true;
    }

    protected override Vector3 EvaluatePathPoint(TransitItemState state, float progress)
    {
        var start = new Vector3(-CellSize * 0.38f, ItemHeight + 0.03f, 0.0f);
        var end = new Vector3(CellSize * 0.34f, ItemHeight, 0.0f);
        return start.Lerp(end, progress);
    }
}
