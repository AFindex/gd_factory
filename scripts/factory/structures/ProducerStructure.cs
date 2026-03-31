using Godot;

public partial class ProducerStructure : FactoryStructure
{
    private double _cooldown;
    private MeshInstance3D? _indicator;

    public override BuildPrototypeKind Kind => BuildPrototypeKind.Producer;

    public override string Description => "Spawner feeding one item forward every few ticks.";

    public override void SimulationStep(SimulationController simulation, double stepSeconds)
    {
        _cooldown -= stepSeconds;

        if (_cooldown > 0.0)
        {
            return;
        }

        var item = simulation.CreateItem(Kind);
        if (simulation.TrySendItem(this, GetOutputCell(), item))
        {
            _cooldown = FactoryConstants.ProducerSpawnSeconds;
            if (_indicator is not null)
            {
                _indicator.Scale = new Vector3(1.0f, 1.2f, 1.0f);
            }
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
        CreateBox("Base", new Vector3(CellSize * 0.9f, 0.8f, CellSize * 0.9f), new Color("6D8B74"), new Vector3(0.0f, 0.4f, 0.0f));
        CreateBox("Tower", new Vector3(CellSize * 0.45f, 1.4f, CellSize * 0.45f), new Color("9DC08B"), new Vector3(-0.15f, 1.1f, 0.0f));
        _indicator = CreateBox("Outlet", new Vector3(CellSize * 0.35f, 0.2f, CellSize * 0.35f), new Color("D7FFC2"), new Vector3(CellSize * 0.45f, 0.75f, 0.0f));
    }
}
