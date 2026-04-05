using Godot;
using System.Collections.Generic;

public partial class PowerPoleStructure : FactoryStructure, IFactoryPowerNode
{
    private MeshInstance3D? _powerRange;
    private MeshInstance3D? _pole;
    private MeshInstance3D? _crossbar;
    private MeshInstance3D? _lamp;
    private double _deployElapsed;

    public override BuildPrototypeKind Kind => BuildPrototypeKind.PowerPole;
    public override string Description => "延伸电网覆盖，把发电机的供电范围传递给更远的机器。";
    public int PowerConnectionRangeCells => 6;

    public override IEnumerable<string> GetInspectionLines()
    {
        foreach (var line in base.GetInspectionLines())
        {
            yield return line;
        }

        yield return $"供电覆盖：半径 {PowerConnectionRangeCells} 格";
    }

    public override void UpdateVisuals(float tickAlpha)
    {
        _deployElapsed = Mathf.Min(0.68, _deployElapsed + (tickAlpha / 60.0f));
        var deployRatio = Mathf.Clamp((float)(_deployElapsed / 0.68f), 0.0f, 1.0f);
        var mastRatio = Mathf.SmoothStep(0.0f, 1.0f, Mathf.Clamp(deployRatio / 0.62f, 0.0f, 1.0f));
        var armRatio = Mathf.SmoothStep(0.0f, 1.0f, Mathf.Clamp((deployRatio - 0.40f) / 0.55f, 0.0f, 1.0f));

        if (_pole is not null)
        {
            _pole.Scale = new Vector3(1.0f, Mathf.Max(0.02f, mastRatio), 1.0f);
            _pole.Position = new Vector3(0.0f, 0.74f * mastRatio, 0.0f);
        }

        if (_crossbar is not null)
        {
            _crossbar.Scale = new Vector3(Mathf.Max(0.08f, armRatio), 1.0f, 1.0f);
            _crossbar.Position = new Vector3(0.0f, 1.06f + (0.36f * mastRatio), 0.0f);
        }

        if (_lamp is not null)
        {
            var lampRise = Mathf.SmoothStep(0.0f, 1.0f, Mathf.Clamp((deployRatio - 0.56f) / 0.44f, 0.0f, 1.0f));
            _lamp.Scale = new Vector3(0.65f + (lampRise * 0.35f), 0.65f + (lampRise * 0.35f), 0.65f + (lampRise * 0.35f));
            _lamp.Position = new Vector3(0.0f, 1.14f + (0.48f * lampRise), 0.0f);
            if (_lamp.MaterialOverride is StandardMaterial3D lampMaterial)
            {
                lampMaterial.EmissionEnabled = true;
                lampMaterial.Emission = new Color("FDE68A");
                lampMaterial.EmissionEnergyMultiplier = 0.3f + (lampRise * 1.6f);
            }
        }
    }

    public override void SetPowerRangeVisible(bool visible)
    {
        if (_powerRange is not null)
        {
            _powerRange.Visible = visible;
        }
    }

    protected override void BuildVisuals()
    {
        _powerRange = CreateDisc(
            "PowerRange",
            CellSize * PowerConnectionRangeCells,
            0.03f,
            new Color(0.99f, 0.88f, 0.42f, 0.12f),
            new Vector3(0.0f, 0.02f, 0.0f));
        _powerRange.Visible = false;

        CreateBox("Footing", new Vector3(CellSize * 0.32f, 0.12f, CellSize * 0.32f), new Color("475569"), new Vector3(0.0f, 0.06f, 0.0f));
        CreateBox("SupportBase", new Vector3(CellSize * 0.22f, 0.18f, CellSize * 0.22f), new Color("78716C"), new Vector3(0.0f, 0.18f, 0.0f));
        _pole = CreateBox("Pole", new Vector3(CellSize * 0.12f, 1.48f, CellSize * 0.12f), new Color("A16207"), new Vector3(0.0f, 0.74f, 0.0f));
        CreateBox("BraceNorth", new Vector3(CellSize * 0.06f, 0.46f, CellSize * 0.06f), new Color("B45309"), new Vector3(0.11f, 0.42f, 0.08f));
        CreateBox("BraceSouth", new Vector3(CellSize * 0.06f, 0.46f, CellSize * 0.06f), new Color("B45309"), new Vector3(-0.11f, 0.42f, -0.08f));
        _crossbar = CreateBox("Crossbar", new Vector3(CellSize * 0.62f, 0.10f, CellSize * 0.10f), new Color("FDE68A"), new Vector3(0.0f, 1.42f, 0.0f));
        _lamp = CreateBox("Lamp", new Vector3(CellSize * 0.12f, 0.12f, CellSize * 0.12f), new Color("FEF08A"), new Vector3(0.0f, 1.62f, 0.0f));

        _deployElapsed = 0.0;
        if (_pole is not null)
        {
            _pole.Scale = new Vector3(1.0f, 0.02f, 1.0f);
            _pole.Position = new Vector3(0.0f, 0.03f, 0.0f);
        }

        if (_crossbar is not null)
        {
            _crossbar.Scale = new Vector3(0.08f, 1.0f, 1.0f);
            _crossbar.Position = new Vector3(0.0f, 1.06f, 0.0f);
        }

        if (_lamp is not null)
        {
            _lamp.Scale = Vector3.One * 0.65f;
            _lamp.Position = new Vector3(0.0f, 1.14f, 0.0f);
        }
    }
}
