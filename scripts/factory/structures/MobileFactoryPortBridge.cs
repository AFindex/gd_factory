using Godot;
using NetFactory.Models;

public partial class MobileFactoryPortBridge : FlowTransportStructure
{
    private GridManager? _worldSite;
    private Vector2I _worldSourceCell;
    private Vector2I _worldTargetCell;
    private bool _hasBinding;
    private int _transitRecycleTotal;

    public override BuildPrototypeKind Kind => BuildPrototypeKind.Belt;
    public override string Description => "移动工厂内部物流对外输出的部署端口；未部署时会自动转入内部回收，避免整条线堵死。";
    public bool IsConnectedToWorld => _hasBinding;
    public Vector2I WorldSourceCell => _worldSourceCell;
    public Vector2I WorldTargetCell => _worldTargetCell;
    public int TransitRecycleTotal => _transitRecycleTotal;

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
        var builder = new DefaultModelBuilder(this, CellSize);
        PortBridgeModelDescriptor.BuildModel(builder, SiteKind);
    }

    protected override bool TryResolveTargetCell(FactoryItem item, Vector2I sourceCell, SimulationController simulation, out Vector2I targetCell)
    {
        targetCell = GetOutputCell();
        return true;
    }

    protected override bool TryDispatchItem(TransitItemState state, SimulationController simulation)
    {
        if (_hasBinding && _worldSite is not null)
        {
            return simulation.TrySendItemToSite(this, _worldSourceCell, _worldSite, _worldTargetCell, state.Item);
        }

        _transitRecycleTotal++;
        return true;
    }

    protected override Vector3 EvaluatePathPoint(TransitItemState state, float progress)
    {
        var start = new Vector3(-CellSize * 0.34f, ItemHeight, 0.0f);
        var end = new Vector3(CellSize * 0.34f, ItemHeight + 0.05f, 0.0f);
        return start.Lerp(end, progress);
    }
}
