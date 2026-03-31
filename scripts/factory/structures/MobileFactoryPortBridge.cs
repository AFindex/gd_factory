using Godot;

public partial class MobileFactoryPortBridge : FlowTransportStructure
{
    private GridManager? _worldSite;
    private Vector2I _worldSourceCell;
    private Vector2I _worldTargetCell;
    private bool _hasBinding;

    public override BuildPrototypeKind Kind => BuildPrototypeKind.Belt;
    public override string Description => "移动工厂内部物流对外输出的部署端口。";
    public bool IsConnectedToWorld => _hasBinding;
    public Vector2I WorldSourceCell => _worldSourceCell;
    public Vector2I WorldTargetCell => _worldTargetCell;

    public void BindToWorld(GridManager worldSite, Vector2I worldSourceCell, FacingDirection facing)
    {
        _worldSite = worldSite;
        _worldSourceCell = worldSourceCell;
        _worldTargetCell = worldSourceCell + FactoryDirection.ToCellOffset(facing);
        _hasBinding = true;
    }

    public void ClearBinding()
    {
        _worldSite = null;
        _worldSourceCell = Vector2I.Zero;
        _worldTargetCell = Vector2I.Zero;
        _hasBinding = false;
    }

    public override bool CanReceiveFrom(Vector2I sourceCell)
    {
        return sourceCell == GetInputCell();
    }

    public override bool CanOutputTo(Vector2I targetCell)
    {
        return false;
    }

    protected override void BuildVisuals()
    {
        CreateColoredBox("Pad", new Vector3(CellSize * 0.72f, 0.14f, CellSize * 0.72f), new Color("475569"), new Vector3(0.0f, 0.08f, 0.0f));
        CreateColoredBox("Chute", new Vector3(CellSize * 0.68f, 0.18f, CellSize * 0.22f), new Color("F97316"), new Vector3(0.08f * CellSize, 0.18f, 0.0f));
        CreateColoredBox("Beacon", new Vector3(CellSize * 0.16f, 0.12f, CellSize * 0.16f), new Color("FED7AA"), new Vector3(CellSize * 0.28f, 0.28f, 0.0f));
    }

    protected override bool TryResolveTargetCell(FactoryItem item, Vector2I sourceCell, SimulationController simulation, out Vector2I targetCell)
    {
        targetCell = GetOutputCell();
        return _hasBinding;
    }

    protected override bool TryDispatchItem(TransitItemState state, SimulationController simulation)
    {
        return _hasBinding
            && _worldSite is not null
            && simulation.TrySendItemToSite(this, _worldSourceCell, _worldSite, _worldTargetCell, state.Item);
    }

    protected override Vector3 EvaluatePathPoint(TransitItemState state, float progress)
    {
        var start = new Vector3(-CellSize * 0.34f, ItemHeight, 0.0f);
        var end = new Vector3(CellSize * 0.34f, ItemHeight + 0.05f, 0.0f);
        return start.Lerp(end, progress);
    }
}
