using Godot;
using NetFactory.Models;
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
        var builder = new DefaultModelBuilder(this, CellSize);
        PowerPoleModelDescriptor.BuildModel(builder, SiteKind, GetInteriorVisualRole());
        _powerRange = builder.Root.FindChild("PowerRange", true, false) as MeshInstance3D;
        _pole = builder.Root.FindChild("Pole", true, false) as MeshInstance3D;
        _crossbar = builder.Root.FindChild("Crossbar", true, false) as MeshInstance3D;
        _lamp = builder.Root.FindChild("Lamp", true, false) as MeshInstance3D;

        if (_powerRange is not null)
        {
            _powerRange.Visible = false;
        }

        _deployElapsed = 0.0;
    }
}
