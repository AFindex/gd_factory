using Godot;

public partial class SinkStructure : FactoryStructure
{
    private int _recentDelivered;
    private double _rateTimer;
    private MeshInstance3D? _indicator;

    public int DeliveredTotal { get; private set; }
    public int DeliveredRate { get; private set; }

    public override BuildPrototypeKind Kind => BuildPrototypeKind.Sink;

    public override string Description => "Destination hub counting delivered items.";

    public override bool TryAcceptItem(FactoryItem item, Vector2I sourceCell, SimulationController simulation)
    {
        if (!AcceptsFrom(sourceCell))
        {
            return false;
        }

        DeliveredTotal++;
        _recentDelivered++;

        if (_indicator is not null)
        {
            _indicator.Scale = new Vector3(1.15f, 1.15f, 1.15f);
        }

        return true;
    }

    public override void SimulationStep(SimulationController simulation, double stepSeconds)
    {
        _rateTimer += stepSeconds;

        if (_rateTimer >= 1.0)
        {
            DeliveredRate = _recentDelivered;
            _recentDelivered = 0;
            _rateTimer = 0.0;
        }
    }

    public override void UpdateVisuals(float tickAlpha)
    {
        if (_indicator is not null)
        {
            _indicator.Scale = _indicator.Scale.Lerp(Vector3.One, tickAlpha * 0.5f);
        }
    }

    protected override void BuildVisuals()
    {
        CreateBox("Base", new Vector3(CellSize * 0.95f, 0.7f, CellSize * 0.95f), new Color("334155"), new Vector3(0.0f, 0.35f, 0.0f));
        CreateBox("Bin", new Vector3(CellSize * 0.72f, 1.1f, CellSize * 0.72f), new Color("94A3B8"), new Vector3(0.0f, 1.0f, 0.0f));
        _indicator = CreateBox("Beacon", new Vector3(CellSize * 0.25f, 0.25f, CellSize * 0.25f), new Color("FDE68A"), new Vector3(-CellSize * 0.3f, 1.55f, 0.0f));
    }
}
