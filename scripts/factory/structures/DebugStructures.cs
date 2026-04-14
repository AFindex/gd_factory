using Godot;
using System.Collections.Generic;

public abstract partial class DebugItemSourceStructure : FactoryRecipeMachineStructure
{
    private readonly IReadOnlyList<FactoryRecipeDefinition> _recipes;
    private MeshInstance3D? _statusLamp;
    private Node3D? _spinnerRig;

    protected DebugItemSourceStructure(IReadOnlyList<FactoryRecipeDefinition> recipes)
        : base(1, 1, 4, 3)
    {
        _recipes = recipes;
    }

    protected override IReadOnlyList<FactoryRecipeDefinition> AvailableRecipes => _recipes;
    protected override string DetailSubtitle => "零成本调试供料";
    protected override string OutputSectionTitle => "调试输出缓存";
    protected override string RecipeSectionTitle => "调试输出";
    protected override string RecipeSectionDescription => "切换当前零成本调试输出物品，无需上游输入。";

    public override IEnumerable<string> GetInspectionLines()
    {
        foreach (var line in base.GetInspectionLines())
        {
            yield return line;
        }

        yield return "调试模式：无成本持续供料";
    }

    public override void UpdateVisuals(float tickAlpha)
    {
        if (_spinnerRig is not null)
        {
            var spin = (HasActiveProcess ? 0.06f : HasBufferedOutput ? 0.02f : 0.01f) * tickAlpha * 60.0f;
            _spinnerRig.Rotation += new Vector3(0.0f, spin, 0.0f);
        }

        if (_statusLamp?.MaterialOverride is StandardMaterial3D lampMaterial)
        {
            var pulse = 0.55f + (0.45f * Mathf.Sin((float)(Time.GetTicksMsec() / 180.0)));
            lampMaterial.EmissionEnabled = true;
            lampMaterial.Emission = FactoryPresentation.GetBuildPrototypeAccentColor(Kind);
            lampMaterial.EmissionEnergyMultiplier = 0.55f + (pulse * 1.05f);
        }
    }

    protected override FactoryInteriorVisualRole GetInteriorVisualRole()
    {
        return FactoryInteriorVisualRole.ServiceModule;
    }

    protected override void BuildVisuals()
    {
        var accent = FactoryPresentation.GetBuildPrototypeAccentColor(Kind);
        var dark = accent.Darkened(0.35f);
        var light = accent.Lightened(0.28f);
        var visualRoot = GetNode<Node3D>("StructureVisualRoot");

        if (SiteKind == FactorySiteKind.Interior)
        {
            CreateInteriorModuleShell(visualRoot, "DebugSource", new Vector3(CellSize * 0.64f, 0.78f, CellSize * 0.64f), dark, light, new Vector3(0.0f, 0.42f, 0.0f));
            _spinnerRig = new Node3D { Name = "DebugSpinnerRig", Position = new Vector3(0.0f, 0.84f, 0.0f) };
            visualRoot.AddChild(_spinnerRig);
            CreateBox(_spinnerRig, "DebugSpinnerNorth", new Vector3(CellSize * 0.10f, 0.14f, CellSize * 0.42f), accent, Vector3.Zero);
            CreateBox(_spinnerRig, "DebugSpinnerEast", new Vector3(CellSize * 0.42f, 0.14f, CellSize * 0.10f), light, Vector3.Zero);
            CreateInteriorTray(visualRoot, "DebugSourceTray", new Vector3(CellSize * 0.52f, 0.12f, CellSize * 0.38f), dark.Lightened(0.08f), light, new Vector3(0.0f, 0.20f, 0.0f));
            _statusLamp = CreateBox("DebugStatusLamp", new Vector3(CellSize * 0.12f, CellSize * 0.12f, CellSize * 0.12f), light, new Vector3(0.0f, 1.04f, 0.0f));
            return;
        }

        CreateBox("DebugFooting", new Vector3(CellSize * 0.86f, 0.20f, CellSize * 0.86f), dark, new Vector3(0.0f, 0.10f, 0.0f));
        CreateBox("DebugCrate", new Vector3(CellSize * 0.64f, 0.92f, CellSize * 0.64f), accent, new Vector3(0.0f, 0.56f, 0.0f));
        _spinnerRig = new Node3D { Name = "DebugSpinnerRig", Position = new Vector3(0.0f, 1.08f, 0.0f) };
        visualRoot.AddChild(_spinnerRig);
        CreateBox(_spinnerRig, "DebugSpinnerNorth", new Vector3(CellSize * 0.12f, 0.14f, CellSize * 0.52f), light, Vector3.Zero);
        CreateBox(_spinnerRig, "DebugSpinnerEast", new Vector3(CellSize * 0.52f, 0.14f, CellSize * 0.12f), dark, Vector3.Zero);
        CreateBox("DebugOutlet", new Vector3(CellSize * 0.22f, 0.28f, CellSize * 0.22f), light, new Vector3(CellSize * 0.34f, 0.66f, 0.0f));
        _statusLamp = CreateBox("DebugStatusLamp", new Vector3(CellSize * 0.14f, CellSize * 0.14f, CellSize * 0.14f), light, new Vector3(0.0f, 1.38f, 0.0f));
    }
}

public partial class DebugOreSourceStructure : DebugItemSourceStructure
{
    public DebugOreSourceStructure()
        : base(FactoryRecipeCatalog.DebugOreSourceRecipes)
    {
    }

    public override BuildPrototypeKind Kind => BuildPrototypeKind.DebugOreSource;
    public override string Description => "调试专用原料源，可按所选配方无成本持续产出单种原矿或基础原料。";
}

public partial class DebugPartSourceStructure : DebugItemSourceStructure
{
    public DebugPartSourceStructure()
        : base(FactoryRecipeCatalog.DebugPartSourceRecipes)
    {
    }

    public override BuildPrototypeKind Kind => BuildPrototypeKind.DebugPartSource;
    public override string Description => "调试专用部件源，可按所选配方无成本持续产出单种板材或中间件。";
}

public partial class DebugCombatSourceStructure : DebugItemSourceStructure
{
    public DebugCombatSourceStructure()
        : base(FactoryRecipeCatalog.DebugCombatSourceRecipes)
    {
    }

    public override BuildPrototypeKind Kind => BuildPrototypeKind.DebugCombatSource;
    public override string Description => "调试专用战备源，可按所选配方无成本持续产出单种战备或维护补给。";
}

public partial class DebugPowerGeneratorStructure : FactoryStructure, IFactoryPowerProducer
{
    private Node3D? _rotorRig;
    private MeshInstance3D? _statusLamp;
    private MeshInstance3D? _powerRange;

    public override BuildPrototypeKind Kind => BuildPrototypeKind.DebugPowerGenerator;
    public override string Description => "调试专用永久供电机，无需燃料即可持续输出稳定电力。";
    public int PowerConnectionRangeCells => 6;
    public float NominalPowerSupply => 96.0f;

    public float GetAvailablePower(SimulationController simulation)
    {
        return NominalPowerSupply;
    }

    public override IEnumerable<string> GetInspectionLines()
    {
        foreach (var line in base.GetInspectionLines())
        {
            yield return line;
        }

        yield return $"发电：{NominalPowerSupply:0} kW 稳定输出";
        yield return $"供电覆盖：半径 {PowerConnectionRangeCells} 格";
        yield return "燃料：测试模式，无需补给";
    }

    public override void UpdateVisuals(float tickAlpha)
    {
        if (_rotorRig is not null)
        {
            _rotorRig.Rotation += new Vector3(0.0f, 0.08f * tickAlpha * 60.0f, 0.0f);
        }

        if (_statusLamp?.MaterialOverride is StandardMaterial3D lampMaterial)
        {
            var pulse = 0.7f + (0.3f * Mathf.Sin((float)(Time.GetTicksMsec() / 220.0)));
            lampMaterial.EmissionEnabled = true;
            lampMaterial.Emission = new Color("FDE68A");
            lampMaterial.EmissionEnergyMultiplier = 0.95f + pulse;
        }
    }

    public override void SetPowerRangeVisible(bool visible)
    {
        if (_powerRange is not null)
        {
            _powerRange.Visible = visible;
        }
    }

    protected override FactoryInteriorVisualRole GetInteriorVisualRole()
    {
        return FactoryInteriorVisualRole.PowerNode;
    }

    protected override void BuildVisuals()
    {
        var visualRoot = GetNode<Node3D>("StructureVisualRoot");
        _powerRange = CreateDisc(
            "PowerRange",
            CellSize * PowerConnectionRangeCells,
            0.03f,
            new Color(0.99f, 0.88f, 0.42f, 0.12f),
            new Vector3(0.0f, 0.02f, 0.0f));
        _powerRange.Visible = false;

        if (SiteKind == FactorySiteKind.Interior)
        {
            CreateInteriorModuleShell(visualRoot, "DebugPower", new Vector3(CellSize * 0.68f, 0.86f, CellSize * 0.68f), new Color("4C3B12"), new Color("FBBF24"), new Vector3(0.0f, 0.44f, 0.0f));
            _rotorRig = new Node3D { Name = "DebugPowerRotorRig", Position = new Vector3(0.0f, 0.86f, 0.0f) };
            visualRoot.AddChild(_rotorRig);
            CreateBox(_rotorRig, "RotorBladeNorth", new Vector3(CellSize * 0.12f, 0.12f, CellSize * 0.54f), new Color("FCD34D"), Vector3.Zero);
            CreateBox(_rotorRig, "RotorBladeEast", new Vector3(CellSize * 0.54f, 0.12f, CellSize * 0.12f), new Color("FDE68A"), Vector3.Zero);
            CreateBox("PowerCore", new Vector3(CellSize * 0.26f, 0.38f, CellSize * 0.26f), new Color("F59E0B"), new Vector3(0.0f, 0.54f, 0.0f));
            _statusLamp = CreateBox("PowerLamp", new Vector3(CellSize * 0.14f, CellSize * 0.14f, CellSize * 0.14f), new Color("FEF3C7"), new Vector3(0.0f, 1.08f, 0.0f));
            return;
        }

        CreateBox("Base", new Vector3(CellSize * 0.90f, 0.22f, CellSize * 0.90f), new Color("5B4420"), new Vector3(0.0f, 0.11f, 0.0f));
        CreateBox("GeneratorBody", new Vector3(CellSize * 0.62f, 0.92f, CellSize * 0.62f), new Color("D97706"), new Vector3(0.0f, 0.58f, 0.0f));
        _rotorRig = new Node3D { Name = "DebugPowerRotorRig", Position = new Vector3(0.0f, 1.18f, 0.0f) };
        visualRoot.AddChild(_rotorRig);
        CreateBox(_rotorRig, "RotorBladeNorth", new Vector3(CellSize * 0.12f, 0.12f, CellSize * 0.62f), new Color("FCD34D"), Vector3.Zero);
        CreateBox(_rotorRig, "RotorBladeEast", new Vector3(CellSize * 0.62f, 0.12f, CellSize * 0.12f), new Color("FDE68A"), Vector3.Zero);
        CreateBox("GeneratorCore", new Vector3(CellSize * 0.28f, 0.42f, CellSize * 0.28f), new Color("FDBA74"), new Vector3(0.0f, 0.76f, 0.0f));
        _statusLamp = CreateBox("PowerLamp", new Vector3(CellSize * 0.16f, CellSize * 0.16f, CellSize * 0.16f), new Color("FEF3C7"), new Vector3(0.0f, 1.52f, 0.0f));
    }
}
